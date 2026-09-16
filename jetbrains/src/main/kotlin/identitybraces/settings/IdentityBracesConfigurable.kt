package identitybraces.settings

import com.intellij.openapi.options.BoundConfigurable
import com.intellij.openapi.ui.DialogPanel
import com.intellij.ui.dsl.builder.bindIntValue
import com.intellij.ui.dsl.builder.bindItem
import com.intellij.ui.dsl.builder.bindSelected
import com.intellij.ui.dsl.builder.panel
import com.intellij.ui.dsl.builder.toNullableProperty
import com.intellij.util.xmlb.XmlSerializerUtil

/**
 * Settings → Editor → Identity Braces. The scalars; the eighty-six traits have a page of
 * their own in [TraitsConfigurable].
 *
 * Edits a detached copy of the state and writes it back on Apply, so Cancel really does
 * cancel.
 */
class IdentityBracesConfigurable : BoundConfigurable("Identity Braces") {
    private val state = IdentityBracesSettings.instance.copyState()

    override fun createPanel(): DialogPanel = panel {
        group("Braces") {
            row {
                checkBox("Enabled").bindSelected(state::enabled)
                    .comment("Master switch. Also available as Tools → Identity Braces → Toggle.")
            }
            row {
                checkBox("Every brace is its own person").bindSelected(state::independentBraces)
                    .comment("A closing brace gets its own colour and personality instead of inheriting its opener's. Turn off to make pairs match again.")
            }
            row("Colour:") {
                checkBox("Curly braces").bindSelected(state::colorCurlyBraces)
                checkBox("Parentheses").bindSelected(state::colorParentheses)
                checkBox("Square brackets").bindSelected(state::colorSquareBrackets)
            }
            row {
                checkBox("Unmatched braces question themselves").bindSelected(state::questionUnmatched)
                    .comment("A brace with no partner always renders as ?. Doubles as a syntax-error hint.")
            }
            row("Maximum file length:") {
                spinner(1_000..100_000_000, 100_000).bindIntValue(state::maxFileLength)
                    .comment("Files longer than this are left alone entirely.")
            }
        }

        group("Motion") {
            row {
                checkBox("Enable motion").bindSelected(state::enableMotion)
                    .comment("Turn this off on battery, on a projector, or if movement in a text editor is unpleasant. Animated braces go static, the ? stops wobbling.")
            }
            row("Animation frame rate:") {
                spinner(1..60).bindIntValue(state::animationFrameRate)
                    .comment("Frames per second. The editor does not need 60.")
            }
            row("Colour cycle seconds:") {
                spinner(1..120).bindIntValue(state::cycleSeconds)
                    .comment("Time for an animated brace to travel the whole wheel.")
            }
            row("Scene interval (seconds):") {
                spinner(1..600).bindIntValue(state::sceneIntervalSeconds)
                    .comment("How often an editor tries to play a multi-brace scene. Most attempts find nothing eligible and do nothing.")
            }
        }

        group("Costume") {
            row("Thigh highs:") {
                comboBox(StockingStyle.entries).bindItem(state::stocking.toNullableProperty())
                    .comment("How much stripe detail the stockings carry. Garter is the legibility floor at 10pt.")
            }
            row {
                checkBox("Tail").bindSelected(state::catgirlTail)
                    .comment("A tail curling off the bottom right. Overhangs the next cell slightly; it is what makes the glyph read as a creature.")
            }
            row("Decoration size (%):") {
                spinner(25..400, 5).bindIntValue(state::decorScalePercent)
                    .comment("Multiplier for everything drawn on a brace — ears, hats, tails — independent of the font.")
            }
            row("Render mode:") {
                comboBox(RenderMode.entries).bindItem(state::renderMode.toNullableProperty())
                    .comment("Full draws everything. Text keeps substitutions and motion but nothing drawn beside the glyph. Plain is colours only. A troubleshooting ladder, not a feature.")
            }
        }

        group("Reading the code again") {
            row("Colour mode:") {
                comboBox(BraceColorMode.entries).bindItem(state::colorMode.toNullableProperty())
                    .comment("Palette: 32 identity colours. Monochrome: the editor's text colour. Depth: one colour per nesting level, so a pair agrees with itself.")
            }
            row {
                checkBox("Scope spotlight").bindSelected(state::scopeSpotlight)
                    .comment("The pair enclosing the caret stays vivid; every other brace fades.")
            }
            row("Spotlight dim (%):") {
                spinner(5..100, 5).bindIntValue(state::spotlightDimPercent)
                    .comment("How visible an out-of-scope brace stays.")
            }
            row("Complexity warning depth:") {
                spinner(0..64).bindIntValue(state::complexityWarningDepth)
                    .comment("Braces nested at least this deep are drawn sweating and unsteady. 0 is off; the shallowest meaningful threshold is 2.")
            }
            row {
                checkBox("Coloured indent guides").bindSelected(state::indentGuides)
                    .comment("A vertical guide down the inside of each multi-line pair, in that pair's colour.")
            }
            row("Indent guide strength (%):") {
                spinner(5..100, 5).bindIntValue(state::indentGuideOpacityPercent)
            }
        }

        row {
            comment("The 32 palette entries are editable under Editor → Color Scheme → Identity Braces. The trait catalogue — the creatures, costumes and motions — is on the Traits page.")
        }
    }

    override fun reset() {
        XmlSerializerUtil.copyBean(IdentityBracesSettings.instance.copyState(), state)
        super.reset()
    }

    override fun apply() {
        super.apply()
        val settings = IdentityBracesSettings.instance
        // The traits are owned by their own page; carry the current ones across untouched.
        state.traitWeights = settings.traitWeightMap()
        settings.update(state)
    }
}
