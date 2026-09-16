package identitybraces.settings

import identitybraces.core.TraitCatalog
import identitybraces.core.TraitIds
import identitybraces.render.TraitDrawing
import java.io.File

/**
 * Reads the Visual Studio extension's `settings.ini` and turns it into this plugin's state.
 *
 * Both extensions describe the same braces, so nobody should have to decide twice which of
 * the eighty-six traits they want. Two things it does deliberately, both inherited from the
 * VS Code port's importer:
 *
 * - **It maps every setting, including the ones that already match the default.** An import
 *   that only wrote the differences would leave whatever was here before in charge of the
 *   rest, and "port my settings" would quietly mean "port some of them".
 * - **It writes every trait, including the zeroes.** Visual Studio only records the traits
 *   you have turned *on* and treats everything absent as zero; this plugin falls back to the
 *   catalogue default instead. Copying only what was in the file would switch `question`,
 *   `catgirl`, `cycle` and `thighhighs` back on for anyone who had deliberately switched
 *   them off.
 */
object SettingsImport {
    class Result(val state: IdentityBracesSettings.State, val notes: List<String>)

    /** Where the Visual Studio extension keeps its settings. Null when there is no %APPDATA%. */
    fun defaultPath(): File? {
        val appData = System.getenv("APPDATA") ?: return null
        return File(appData, "IdentityBraces${File.separator}settings.ini")
    }

    fun parse(lines: List<String>): Map<String, String> {
        val values = HashMap<String, String>()
        for (line in lines) {
            val trimmed = line.trim()
            if (trimmed.isEmpty() || trimmed[0] == '#') {
                continue
            }

            val split = trimmed.indexOf('=')
            if (split <= 0) {
                continue
            }

            values[trimmed.substring(0, split).trim().lowercase()] = trimmed.substring(split + 1).trim()
        }

        return values
    }

    fun convert(values: Map<String, String>, current: IdentityBracesSettings.State): Result {
        val state = IdentityBracesSettings.State()
        val notes = ArrayList<String>()

        fun bool(key: String, fallback: Boolean): Boolean = values[key.lowercase()]?.toBooleanStrictOrNull() ?: fallback
        fun int(key: String, fallback: Int): Int = values[key.lowercase()]?.toIntOrNull() ?: fallback

        state.enabled = bool("Enabled", true)
        state.colorCurlyBraces = bool("ColorCurlyBraces", true)
        state.colorParentheses = bool("ColorParentheses", true)
        state.colorSquareBrackets = bool("ColorSquareBrackets", true)
        state.enableMotion = bool("EnableMotion", true)
        state.animationFrameRate = int("AnimationFrameRate", 18)
        state.cycleSeconds = int("CycleSeconds", 6)
        state.sceneIntervalSeconds = int("SceneIntervalSeconds", 8)
        state.questionUnmatched = bool("QuestionUnmatched", true)
        state.independentBraces = bool("IndependentBraces", true)
        state.colorMode = when (int("ColorMode", 0)) {
            1 -> BraceColorMode.Monochrome
            2 -> BraceColorMode.Depth
            else -> BraceColorMode.Palette
        }
        state.scopeSpotlight = bool("ScopeSpotlight", false)
        state.spotlightDimPercent = int("SpotlightDimPercent", 30)
        state.complexityWarningDepth = int("ComplexityWarningDepth", 0)
        state.indentGuides = bool("IndentGuides", false)
        state.indentGuideOpacityPercent = int("IndentGuideOpacityPercent", 45)
        state.decorScalePercent = int("EarScalePercent", 100)
        state.stocking = when (int("Stocking", 2)) {
            0 -> StockingStyle.Off
            1 -> StockingStyle.TwoTone
            3 -> StockingStyle.Banded
            else -> StockingStyle.Garter
        }
        state.catgirlTail = bool("CatgirlTail", true)
        state.maxFileLength = int("MaxFileLength", 1_000_000)
        state.renderMode = current.renderMode

        // The weight table: every trait, absent ones at zero. A file from before the trait
        // table existed carries the three original percentages instead.
        val weights = LinkedHashMap<String, Int>()
        var sawAny = false
        for (info in TraitCatalog.all) {
            val raw = values["trait.${info.id}".lowercase()]?.toIntOrNull()
            if (raw != null) {
                sawAny = true
            }

            weights[info.id] = raw ?: 0
        }

        if (!sawAny) {
            weights[TraitIds.COLOUR_CYCLE] = int("AnimatedPercent", 8)
            weights[TraitIds.QUESTION] = int("QuestioningPercent", 6)
            weights[TraitIds.CATGIRL] = int("CatgirlPercent", 4)
            weights[TraitIds.THIGH_HIGHS] = weights[TraitIds.CATGIRL] ?: 0
        }

        state.traitWeights = weights

        // Say what did not come across rather than leaving it to be discovered.
        if (values.containsKey("reserveearspace") && values["reserveearspace"].equals("true", ignoreCase = true)) {
            notes.add("Reserve space for cat ears has no counterpart here; the ears overhang the line above.")
        }

        if (values.containsKey("bracescalepercent") && int("BraceScalePercent", 100) != 100) {
            notes.add("Brace scale is not supported here; use the editor's own font size.")
        }

        for (info in TraitCatalog.all) {
            val weight = weights[info.id] ?: 0
            if (weight > 0 && !TraitDrawing.isImplemented(info.id)) {
                notes.add("${info.name} is set to $weight but draws nothing in this port.")
            }
        }

        IdentityBracesSettings.normalize(state)
        return Result(state, notes)
    }
}
