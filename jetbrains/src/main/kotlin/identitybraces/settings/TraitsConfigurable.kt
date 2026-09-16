package identitybraces.settings

import com.intellij.openapi.options.Configurable
import com.intellij.ui.JBColor
import com.intellij.ui.SearchTextField
import com.intellij.ui.components.JBLabel
import com.intellij.ui.components.JBScrollPane
import com.intellij.util.ui.JBUI
import com.intellij.util.ui.UIUtil
import identitybraces.core.TraitCatalog
import identitybraces.core.TraitInfo
import identitybraces.core.TraitLayer
import identitybraces.core.TraitPresets
import identitybraces.core.TraitSampler
import identitybraces.render.StyleSettings
import identitybraces.render.TraitDrawing
import java.awt.BorderLayout
import java.awt.Dimension
import java.awt.FlowLayout
import java.awt.GridBagConstraints
import java.awt.GridBagLayout
import java.awt.event.MouseAdapter
import java.awt.event.MouseEvent
import javax.swing.BorderFactory
import javax.swing.Box
import javax.swing.BoxLayout
import javax.swing.JButton
import javax.swing.JComponent
import javax.swing.JPanel
import javax.swing.JSlider
import javax.swing.JSpinner
import javax.swing.SpinnerNumberModel
import javax.swing.event.DocumentEvent
import javax.swing.event.DocumentListener

/**
 * Settings → Editor → Identity Braces → Traits: the catalogue of eighty-six, one slider each,
 * grouped by layer.
 *
 * Two things about it are worth knowing. **The layer totals are live**: body, creature,
 * costume and motion share a single 0–100 roll, so a layer summing past 100 gets scaled
 * down on Apply and the traits at the bottom of it become unreachable; the header shows
 * `n / 100` and turns orange when you go over, which used to be a silent failure. And
 * **both previews are the real renderer**, so they cannot drift from what you will get.
 *
 * Everything on the page edits a copy. Cancel really does cancel.
 */
class TraitsConfigurable : Configurable {
    private var working: MutableMap<String, Int> = IdentityBracesSettings.instance.traitWeightMap()

    private class Row(val info: TraitInfo, val panel: JPanel, val slider: JSlider, val spinner: JSpinner)

    private val rows = ArrayList<Row>()
    private val headers = HashMap<TraitLayer, JBLabel>()
    private var selected: TraitInfo? = TraitCatalog.all.first()
    private var syncing = false

    private val magnified = BracePreview(magnification = 3f, animated = true, cellsPerBrace = 3)
    private val strip = BracePreview(magnification = 1f, animated = false, cellsPerBrace = 2)
    private val selectedTitle = JBLabel()

    override fun getDisplayName(): String = "Traits"

    override fun createComponent(): JComponent {
        val root = JPanel(BorderLayout(0, JBUI.scale(8)))

        // ---- search and presets ----

        val top = JPanel(BorderLayout(JBUI.scale(8), 0))
        val search = SearchTextField()
        search.textEditor.emptyText.text = "Search traits"
        search.addDocumentListener(object : DocumentListener {
            override fun insertUpdate(e: DocumentEvent) = filter(search.text)
            override fun removeUpdate(e: DocumentEvent) = filter(search.text)
            override fun changedUpdate(e: DocumentEvent) = filter(search.text)
        })
        top.add(search, BorderLayout.CENTER)

        val presets = JPanel(FlowLayout(FlowLayout.RIGHT, JBUI.scale(4), 0))
        for (preset in TraitPresets.all) {
            val button = JButton(preset.name)
            button.toolTipText = preset.description
            button.addActionListener {
                working = LinkedHashMap(TraitPresets.weightsOf(preset))
                syncFromWorking()
            }
            presets.add(button)
        }
        top.add(presets, BorderLayout.EAST)
        root.add(top, BorderLayout.NORTH)

        // ---- the sliders ----

        val list = JPanel()
        list.layout = BoxLayout(list, BoxLayout.Y_AXIS)

        for (layer in TraitLayer.entries) {
            val header = JBLabel()
            header.font = header.font.deriveFont(java.awt.Font.BOLD)
            header.border = JBUI.Borders.empty(10, 4, 4, 4)
            header.alignmentX = 0f
            headers[layer] = header
            list.add(header)

            for (info in TraitCatalog.all.filter { it.layer == layer }) {
                val row = buildRow(info)
                rows.add(row)
                list.add(row.panel)
            }
        }

        list.add(Box.createVerticalGlue())
        val scroll = JBScrollPane(list)
        scroll.verticalScrollBar.unitIncrement = JBUI.scale(16)
        scroll.border = BorderFactory.createEmptyBorder()
        root.add(scroll, BorderLayout.CENTER)

        // ---- the previews ----

        val previews = JPanel(BorderLayout(JBUI.scale(8), 0))

        val magnifiedBox = JPanel(BorderLayout())
        magnifiedBox.border = BorderFactory.createTitledBorder("Selected trait, magnified")
        selectedTitle.border = JBUI.Borders.empty(0, 4, 4, 4)
        magnifiedBox.add(selectedTitle, BorderLayout.NORTH)
        magnified.preferredSize = Dimension(JBUI.scale(180), JBUI.scale(110))
        magnifiedBox.add(magnified, BorderLayout.CENTER)
        previews.add(magnifiedBox, BorderLayout.WEST)

        val stripBox = JPanel(BorderLayout())
        stripBox.border = BorderFactory.createTitledBorder("At these weights, actual size — 120 braces rolled through the real table")
        strip.preferredSize = Dimension(JBUI.scale(400), JBUI.scale(110))
        stripBox.add(strip, BorderLayout.CENTER)
        previews.add(stripBox, BorderLayout.CENTER)

        root.add(previews, BorderLayout.SOUTH)

        syncFromWorking()
        return root
    }

    private fun buildRow(info: TraitInfo): Row {
        val panel = JPanel(GridBagLayout())
        panel.alignmentX = 0f
        panel.border = JBUI.Borders.empty(1, 4)
        panel.maximumSize = Dimension(Int.MAX_VALUE, JBUI.scale(30))

        val c = GridBagConstraints()
        c.gridy = 0
        c.insets = JBUI.insets(0, 2)
        c.anchor = GridBagConstraints.WEST

        val name = JBLabel(info.name)
        name.preferredSize = Dimension(JBUI.scale(130), name.preferredSize.height)
        c.gridx = 0
        c.weightx = 0.0
        c.fill = GridBagConstraints.NONE
        panel.add(name, c)

        val slider = JSlider(0, 100, working[info.id] ?: 0)
        slider.preferredSize = Dimension(JBUI.scale(160), slider.preferredSize.height)
        c.gridx = 1
        panel.add(slider, c)

        val spinner = JSpinner(SpinnerNumberModel(working[info.id] ?: 0, 0, 100, 1))
        spinner.preferredSize = Dimension(JBUI.scale(60), spinner.preferredSize.height)
        c.gridx = 2
        panel.add(spinner, c)

        val describe = if (TraitDrawing.isImplemented(info.id)) info.description else "${info.description}  (not in this port)"
        val description = JBLabel(describe)
        description.foreground = UIUtil.getContextHelpForeground()
        description.font = JBUI.Fonts.smallFont()
        c.gridx = 3
        c.weightx = 1.0
        c.fill = GridBagConstraints.HORIZONTAL
        panel.add(description, c)

        slider.addChangeListener {
            if (!syncing) {
                set(info, slider.value)
                syncing = true
                spinner.value = slider.value
                syncing = false
            }
        }

        spinner.addChangeListener {
            if (!syncing) {
                set(info, spinner.value as Int)
                syncing = true
                slider.value = spinner.value as Int
                syncing = false
            }
        }

        val select = object : MouseAdapter() {
            override fun mousePressed(e: MouseEvent) = select(info)
        }
        panel.addMouseListener(select)
        name.addMouseListener(select)
        slider.addMouseListener(select)

        return Row(info, panel, slider, spinner)
    }

    private fun set(info: TraitInfo, percent: Int) {
        working[info.id] = percent.coerceIn(0, 100)
        select(info)
        updateHeaders()
        updateStrip()
    }

    private fun select(info: TraitInfo) {
        selected = info
        selectedTitle.text = "${info.name} — ${info.description}"
        val traits = TraitSampler.single(info.layer, info.id)
        val seed = TraitSampler.SEED
        magnified.braces = listOf(
            PreviewBrace('{', traits, BracePreview.colorIndexOf(seed), seed),
            PreviewBrace('}', traits, BracePreview.colorIndexOf(seed xor 0x5EA1uL), seed xor 0x5EA1uL),
        )
    }

    private fun updateHeaders() {
        for ((layer, header) in headers) {
            val members = TraitCatalog.all.filter { it.layer == layer }
            if (layer == TraitLayer.Effect) {
                header.text = "Effects — each rolls independently"
                header.foreground = UIUtil.getLabelForeground()
                continue
            }

            val total = members.sumOf { working[it.id] ?: 0 }
            header.text = "${layer.name} — $total / 100" + if (total > 100) "   (over budget: scaled down on Apply)" else ""
            header.foreground = if (total > 100) JBColor.ORANGE else UIUtil.getLabelForeground()
        }
    }

    private fun updateStrip() {
        val weights = TraitCatalog.weightsFrom(working)
        strip.braces = TraitSampler.take(weights, 120).mapIndexed { i, sample ->
            PreviewBrace(if (i % 2 == 0) '{' else '}', sample.traits, BracePreview.colorIndexOf(sample.identity), sample.identity)
        }
    }

    private fun currentStyle(): StyleSettings {
        val settings = IdentityBracesSettings.instance
        return StyleSettings(
            enableMotion = settings.enableMotion,
            cycleSeconds = settings.cycleSeconds,
            spotlightDim = 1.0,
            stocking = settings.stocking,
            tail = settings.catgirlTail,
            renderMode = RenderMode.Full,
            decorScale = settings.decorScalePercent / 100.0,
        )
    }

    private fun syncFromWorking() {
        syncing = true
        try {
            for (row in rows) {
                val value = working[row.info.id] ?: 0
                row.slider.value = value
                row.spinner.value = value
            }
        } finally {
            syncing = false
        }

        val style = currentStyle()
        magnified.styleSettings = style
        strip.styleSettings = StyleSettings(
            enableMotion = false,
            cycleSeconds = style.cycleSeconds,
            spotlightDim = 1.0,
            stocking = style.stocking,
            tail = style.tail,
            renderMode = RenderMode.Full,
            decorScale = style.decorScale,
        )

        updateHeaders()
        updateStrip()
        select(selected ?: TraitCatalog.all.first())
    }

    private fun filter(query: String) {
        val needle = query.trim().lowercase()
        for (row in rows) {
            val info = row.info
            row.panel.isVisible = needle.isEmpty() ||
                info.name.lowercase().contains(needle) ||
                info.id.contains(needle) ||
                info.description.lowercase().contains(needle)
        }
    }

    override fun isModified(): Boolean = working != IdentityBracesSettings.instance.traitWeightMap()

    override fun apply() {
        IdentityBracesSettings.instance.edit { it.traitWeights = LinkedHashMap(working) }
        // Apply normalises, so read back what actually stuck.
        working = IdentityBracesSettings.instance.traitWeightMap()
        syncFromWorking()
    }

    override fun reset() {
        working = IdentityBracesSettings.instance.traitWeightMap()
        if (rows.isNotEmpty()) {
            syncFromWorking()
        }
    }
}
