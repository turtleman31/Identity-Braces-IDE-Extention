package identitybraces.settings

import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.util.messages.Topic
import com.intellij.util.xmlb.XmlSerializerUtil
import identitybraces.core.ScanSettings
import identitybraces.core.TraitCatalog
import identitybraces.core.TraitLayer
import identitybraces.core.TraitPresets
import identitybraces.core.TraitWeight

/** Palette of 32, one theme-following colour, or one colour per nesting level. */
enum class BraceColorMode { Palette, Monochrome, Depth }

/** How much stripe detail the thigh highs carry. */
enum class StockingStyle { Off, TwoTone, Garter, Banded }

/** How much of a personality brace is actually drawn. A troubleshooting ladder, not a feature. */
enum class RenderMode {
    /** Everything: substitutions, transforms, creatures and costumes. */
    Full,

    /** Substitutions and transforms only, nothing drawn beside the glyph. */
    Text,

    /** Colours only. A personality brace keeps its real glyph. */
    Plain,
}

/** Raised on the application bus after any setting changes, so open editors can refresh. */
fun interface IdentityBracesSettingsListener {
    fun settingsChanged(settings: IdentityBracesSettings)

    companion object {
        @JvmField
        val TOPIC: Topic<IdentityBracesSettingsListener> =
            Topic.create("Identity Braces settings", IdentityBracesSettingsListener::class.java)
    }
}

/**
 * The plugin's settings, persisted by the platform in `identityBraces.xml`.
 *
 * The same knobs as the Visual Studio extension's `settings.ini` and the VS Code port's
 * `identityBraces.*`, so [SettingsImport] can carry a setup across. The three descriptions
 * of a brace must agree, and nobody should have to decide twice which of the eighty-six
 * traits they want.
 */
@Service(Service.Level.APP)
@State(name = "IdentityBracesSettings", storages = [Storage("identityBraces.xml")])
class IdentityBracesSettings : PersistentStateComponent<IdentityBracesSettings.State> {
    /** The serialised form. Public mutable fields, because that is what the XML serialiser reads. */
    class State {
        var enabled: Boolean = true

        var colorCurlyBraces: Boolean = true
        var colorParentheses: Boolean = true
        var colorSquareBrackets: Boolean = true

        /**
         * How often each trait comes up, keyed by trait id. Anything absent falls back to
         * the catalogue default, which is how a trait added in a later version turns up at
         * its intended rate rather than at zero.
         */
        var traitWeights: MutableMap<String, Int> = LinkedHashMap()

        /**
         * Master switch for movement. Off leaves animated braces on a static colour and
         * stops the questioning wobble — for battery, for a projector, or for anyone who
         * finds motion in a text editor genuinely unpleasant.
         */
        var enableMotion: Boolean = true

        /** Frames per second for animation. The editor does not need 60. */
        var animationFrameRate: Int = 18

        /** Seconds for an animated brace to travel the full colour wheel. */
        var cycleSeconds: Int = 6

        /** Seconds between attempts to play a multi-brace scene, per editor. */
        var sceneIntervalSeconds: Int = 8

        /** A brace with no partner renders as '?'. Doubles as a syntax-error hint. */
        var questionUnmatched: Boolean = true

        /**
         * A closing brace gets its own identity rather than inheriting its opener's, so `{`
         * and its `}` disagree about colour and about what they are.
         */
        var independentBraces: Boolean = true

        var colorMode: BraceColorMode = BraceColorMode.Palette

        /** The pair enclosing the caret stays vivid; every other brace drops to [spotlightDimPercent]. */
        var scopeSpotlight: Boolean = false

        /** How visible an out-of-scope brace is while the spotlight is on. 5–100. */
        var spotlightDimPercent: Int = 30

        /** Braces nested at least this deep are drawn visibly distressed. Zero switches it off. */
        var complexityWarningDepth: Int = 0

        /** A vertical guide down the inside of every multi-line pair, in that pair's colour. */
        var indentGuides: Boolean = false

        /** How strong the indent guides are. 5–100. */
        var indentGuideOpacityPercent: Int = 45

        /** Percentage size of everything drawn on a brace — ears, hats, tails. 25–400. */
        var decorScalePercent: Int = 100

        var stocking: StockingStyle = StockingStyle.Garter

        /** A tail curling off the bottom right. What makes it read as a creature. */
        var catgirlTail: Boolean = true

        /**
         * Files longer than this are left alone. Every edit produces a full rescan, so this
         * is the guard that keeps typing responsive in a generated monster of a file.
         */
        var maxFileLength: Int = 1_000_000

        var renderMode: RenderMode = RenderMode.Full
    }

    private var state = State()

    /**
     * Bumped on every change. Part of the brace-map cache key, so a settings change
     * invalidates every cached scan without anyone having to enumerate which settings the
     * scanner reads.
     */
    @Volatile
    var version: Int = 0
        private set

    override fun getState(): State = state

    override fun loadState(state: State) {
        XmlSerializerUtil.copyBean(state, this.state)
        normalize(this.state)
        version++
    }

    // ---- reads ----

    val enabled: Boolean get() = state.enabled
    val colorCurlyBraces: Boolean get() = state.colorCurlyBraces
    val colorParentheses: Boolean get() = state.colorParentheses
    val colorSquareBrackets: Boolean get() = state.colorSquareBrackets
    val enableMotion: Boolean get() = state.enableMotion
    val animationFrameRate: Int get() = state.animationFrameRate
    val cycleSeconds: Int get() = state.cycleSeconds
    val sceneIntervalSeconds: Int get() = state.sceneIntervalSeconds
    val questionUnmatched: Boolean get() = state.questionUnmatched
    val independentBraces: Boolean get() = state.independentBraces
    val colorMode: BraceColorMode get() = state.colorMode
    val scopeSpotlight: Boolean get() = state.scopeSpotlight
    val spotlightDimPercent: Int get() = state.spotlightDimPercent
    val complexityWarningDepth: Int get() = state.complexityWarningDepth
    val indentGuides: Boolean get() = state.indentGuides
    val indentGuideOpacityPercent: Int get() = state.indentGuideOpacityPercent
    val decorScalePercent: Int get() = state.decorScalePercent
    val stocking: StockingStyle get() = state.stocking
    val catgirlTail: Boolean get() = state.catgirlTail
    val maxFileLength: Int get() = state.maxFileLength
    val renderMode: RenderMode get() = state.renderMode

    fun traitWeight(id: String): Int {
        return state.traitWeights[id] ?: (TraitCatalog.find(id)?.defaultPercent ?: 0)
    }

    /** Every trait's weight, in catalogue order, as the scanner wants it. */
    fun traitWeights(): List<TraitWeight> = TraitCatalog.all.map { TraitWeight(it.id, it.layer, traitWeight(it.id)) }

    /** The weight table as an explicit map over every catalogue id. */
    fun traitWeightMap(): MutableMap<String, Int> {
        val map = LinkedHashMap<String, Int>()
        for (info in TraitCatalog.all) {
            map[info.id] = traitWeight(info.id)
        }

        return map
    }

    fun toScanSettings(): ScanSettings = ScanSettings(
        curly = colorCurlyBraces,
        round = colorParentheses,
        square = colorSquareBrackets,
        traitWeights = traitWeights(),
        questionUnmatched = questionUnmatched,
        independentBraces = independentBraces,
        colorByDepth = colorMode == BraceColorMode.Depth,
        complexityWarningDepth = complexityWarningDepth,
    )

    /** True if any trait that needs space above the line is enabled. */
    fun anyHeadroomTrait(): Boolean = TraitCatalog.all.any {
        (it.layer == TraitLayer.Creature || it.layer == TraitLayer.Costume) && traitWeight(it.id) > 0
    }

    /** A detached copy for a settings page to edit. Cancel really does cancel. */
    fun copyState(): State {
        val copy = State()
        XmlSerializerUtil.copyBean(state, copy)
        copy.traitWeights = traitWeightMap()
        return copy
    }

    // ---- writes ----

    /** Replaces the whole state, normalises it, and tells every open editor. */
    fun update(newState: State) {
        normalize(newState)
        XmlSerializerUtil.copyBean(newState, state)
        state.traitWeights = LinkedHashMap(newState.traitWeights)
        fireChanged()
    }

    /** One field at a time, for the toggle actions. */
    fun edit(change: (State) -> Unit) {
        val copy = copyState()
        change(copy)
        update(copy)
    }

    /** Overwrites every weight with a preset's, leaving other settings alone. */
    fun applyPreset(presetId: String) {
        val preset = TraitPresets.find(presetId) ?: return
        edit { it.traitWeights = LinkedHashMap(TraitPresets.weightsOf(preset)) }
    }

    private fun fireChanged() {
        version++
        ApplicationManager.getApplication().messageBus.syncPublisher(IdentityBracesSettingsListener.TOPIC).settingsChanged(this)
    }

    companion object {
        @JvmStatic
        val instance: IdentityBracesSettings
            get() = ApplicationManager.getApplication().getService(IdentityBracesSettings::class.java)

        /**
         * Clamps every scalar, and caps each shared-roll layer's weights at a total of 100.
         *
         * Body, Creature, Costume and Motion each share one 0–100 roll, so weights summing
         * past 100 would make the traits at the end of the list unreachable — silently, and
         * only for whoever turned the sliders up. Effects roll independently and are exempt.
         */
        fun normalize(state: State) {
            val weights = state.traitWeights
            for (info in TraitCatalog.all) {
                val value = weights[info.id]
                if (value != null) {
                    weights[info.id] = value.coerceIn(0, 100)
                }
            }

            for (layer in listOf(TraitLayer.Body, TraitLayer.Creature, TraitLayer.Costume, TraitLayer.Motion)) {
                val members = TraitCatalog.all.filter { it.layer == layer }
                val total = members.sumOf { weights[it.id] ?: it.defaultPercent }
                if (total <= 100) {
                    continue
                }

                val scale = 100.0 / total
                for (member in members) {
                    weights[member.id] = ((weights[member.id] ?: member.defaultPercent) * scale).toInt()
                }
            }

            state.animationFrameRate = state.animationFrameRate.coerceIn(1, 60)
            state.cycleSeconds = state.cycleSeconds.coerceIn(1, 120)
            state.sceneIntervalSeconds = state.sceneIntervalSeconds.coerceIn(1, 600)
            state.maxFileLength = state.maxFileLength.coerceIn(1_000, 100_000_000)
            state.spotlightDimPercent = state.spotlightDimPercent.coerceIn(5, 100)
            state.indentGuideOpacityPercent = state.indentGuideOpacityPercent.coerceIn(5, 100)
            state.decorScalePercent = state.decorScalePercent.coerceIn(25, 400)

            // Zero is off. One would distress every brace in the file, which is not a warning
            // about anything, so the shallowest meaningful threshold is two.
            state.complexityWarningDepth = if (state.complexityWarningDepth <= 0) 0 else state.complexityWarningDepth.coerceIn(2, 64)
        }
    }
}
