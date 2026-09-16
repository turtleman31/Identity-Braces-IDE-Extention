package identitybraces.actions

import com.intellij.codeInsight.hint.HintManager
import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.actionSystem.ToggleAction
import com.intellij.openapi.options.ShowSettingsUtil
import com.intellij.openapi.ui.Messages
import com.intellij.openapi.ui.popup.JBPopupFactory
import com.intellij.openapi.ui.popup.PopupStep
import com.intellij.openapi.ui.popup.util.BaseListPopupStep
import identitybraces.core.TraitPreset
import identitybraces.core.TraitPresets
import identitybraces.editor.BraceHover
import identitybraces.editor.EditorBraces
import identitybraces.settings.IdentityBracesSettings
import identitybraces.settings.SettingsImport
import identitybraces.settings.TraitsConfigurable

/** On or off. */
class ToggleEnabledAction : ToggleAction() {
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

    override fun isSelected(e: AnActionEvent): Boolean = IdentityBracesSettings.instance.enabled

    override fun setSelected(e: AnActionEvent, state: Boolean) {
        IdentityBracesSettings.instance.edit { it.enabled = state }
    }
}

/** Colours stay, movement stops. */
class ToggleMotionAction : ToggleAction() {
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

    override fun isSelected(e: AnActionEvent): Boolean = IdentityBracesSettings.instance.enableMotion

    override fun setSelected(e: AnActionEvent, state: Boolean) {
        IdentityBracesSettings.instance.edit { it.enableMotion = state }
    }
}

/** Off / Default / Menagerie / Restless / Unusable. */
class ApplyPresetAction : AnAction() {
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

    override fun actionPerformed(e: AnActionEvent) {
        val step = object : BaseListPopupStep<TraitPreset>("Apply a Trait Preset", TraitPresets.all) {
            override fun getTextFor(value: TraitPreset): String = "${value.name} — ${value.description}"

            override fun onChosen(selectedValue: TraitPreset, finalChoice: Boolean): PopupStep<*>? {
                IdentityBracesSettings.instance.applyPreset(selectedValue.id)
                return FINAL_CHOICE
            }
        }

        JBPopupFactory.getInstance().createListPopup(step).showInBestPositionFor(e.dataContext)
    }
}

/** Name, identity hash, colour, depth and every trait the brace at the caret rolled. */
class WhoIsThisAction : AnAction() {
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.EDT

    override fun update(e: AnActionEvent) {
        e.presentation.isEnabled = e.getData(CommonDataKeys.EDITOR) != null
    }

    override fun actionPerformed(e: AnActionEvent) {
        val editor = e.getData(CommonDataKeys.EDITOR) ?: return
        val controller = EditorBraces.of(editor)
        val map = controller?.map()
        val offset = editor.caretModel.offset

        // The brace under the caret, or the one just before it — a caret usually sits right
        // after the brace someone just typed.
        var index = -1
        if (map != null) {
            val at = map.firstIndexAtOrAfter(offset)
            if (at < map.size && map[at].position == offset) {
                index = at
            } else if (at > 0 && map[at - 1].position == offset - 1) {
                index = at - 1
            }
        }

        val text = if (map == null || index < 0) {
            "No brace at the caret."
        } else {
            BraceHover.describe(map, index)
        }

        HintManager.getInstance().showInformationHint(editor, text)
    }
}

/** Opens the Traits settings page: the eighty-six sliders. */
class OpenTraitsAction : AnAction() {
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

    override fun actionPerformed(e: AnActionEvent) {
        ShowSettingsUtil.getInstance().showSettingsDialog(e.project, TraitsConfigurable::class.java)
    }
}

/**
 * Reads `%APPDATA%\IdentityBraces\settings.ini` — the Visual Studio extension's settings —
 * and applies the equivalents here, then says what did not come across.
 */
class ImportFromVisualStudioAction : AnAction() {
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project
        val file = SettingsImport.defaultPath()
        if (file == null || !file.isFile) {
            Messages.showInfoMessage(
                project,
                "No Visual Studio settings were found at ${file?.path ?: "%APPDATA%\\IdentityBraces\\settings.ini"}.",
                "Import Settings from Visual Studio",
            )
            return
        }

        val settings = IdentityBracesSettings.instance
        val result = SettingsImport.convert(SettingsImport.parse(file.readLines()), settings.copyState())
        settings.update(result.state)

        val summary = if (result.notes.isEmpty()) {
            "Everything came across."
        } else {
            "Imported, with notes:\n\n" + result.notes.joinToString("\n") { "• $it" }
        }

        Messages.showInfoMessage(project, summary, "Import Settings from Visual Studio")
    }
}
