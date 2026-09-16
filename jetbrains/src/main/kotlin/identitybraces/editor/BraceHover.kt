package identitybraces.editor

import com.intellij.codeInsight.hint.HintManager
import com.intellij.codeInsight.hint.HintManagerImpl
import com.intellij.codeInsight.hint.HintUtil
import com.intellij.openapi.editor.event.EditorMouseEvent
import com.intellij.openapi.editor.event.EditorMouseEventArea
import com.intellij.openapi.editor.event.EditorMouseMotionListener
import com.intellij.ui.LightweightHint
import identitybraces.core.BraceInfo
import identitybraces.core.BraceMap
import identitybraces.core.BraceNames
import identitybraces.core.TraitCatalog
import identitybraces.core.TraitIds
import identitybraces.settings.IdentityBracesSettings

/**
 * Introduces a `named` brace when the mouse rests on it: *Sir Reginald the Unclosed*.
 *
 * Through an editor hint rather than a tooltip on the drawn overlay, for the same reason the
 * Visual Studio extension goes through quick info: the overlay sits above the text, and
 * making it hit-testable would have it swallow the clicks that place your caret.
 */
object BraceHover {
    fun install(owner: EditorBraces) {
        val editor = owner.editor
        var shownFor = -1
        var hint: LightweightHint? = null

        editor.addEditorMouseMotionListener(object : EditorMouseMotionListener {
            override fun mouseMoved(e: EditorMouseEvent) {
                if (e.area != EditorMouseEventArea.EDITING_AREA || !IdentityBracesSettings.instance.enabled) {
                    return
                }

                val offset = e.offset
                if (offset == shownFor) {
                    return
                }

                val map = owner.map()
                val index = map.firstIndexAtOrAfter(offset)
                val brace = if (index < map.size && map[index].position == offset) map[index] else null

                if (brace == null || !brace.traits.hasEffect(TraitIds.NAMED)) {
                    if (hint?.isVisible == true) {
                        hint?.hide()
                    }

                    hint = null
                    shownFor = -1
                    return
                }

                shownFor = offset
                val label = HintUtil.createInformationLabel(BraceNames.of(brace.identity))
                val created = LightweightHint(label)
                val point = HintManagerImpl.getHintPosition(created, editor, editor.offsetToLogicalPosition(offset), HintManager.ABOVE)
                HintManagerImpl.getInstanceImpl().showEditorHint(
                    created,
                    editor,
                    point,
                    HintManager.HIDE_BY_ANY_KEY or HintManager.HIDE_BY_TEXT_CHANGE or HintManager.HIDE_BY_SCROLLING,
                    0,
                    false,
                )
                hint = created
            }
        }, owner)
    }

    /** Everything there is to know about one brace, for the "who is this" action. */
    fun describe(map: BraceMap, index: Int): String {
        val brace: BraceInfo = map[index]
        val traits = brace.traits
        val parts = ArrayList<String>()

        parts.add("<b>${BraceNames.of(brace.identity)}</b>")
        parts.add("identity ${brace.identity.toString(16).padStart(16, '0')}")
        parts.add("colour ${brace.colorIndex}, depth ${brace.depth}, ${if (brace.isMatched) "matched" else "unmatched"}")

        val rolled = listOfNotNull(traits.body, traits.creature, traits.costume, traits.motion) + traits.effects
        if (rolled.isEmpty()) {
            parts.add("plain — no traits rolled")
        } else {
            parts.add(rolled.joinToString(", ") { id -> TraitCatalog.find(id)?.name ?: id })
        }

        return parts.joinToString("<br>")
    }
}
