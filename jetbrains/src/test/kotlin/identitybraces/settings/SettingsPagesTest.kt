package identitybraces.settings

import com.intellij.testFramework.fixtures.BasePlatformTestCase
import identitybraces.core.TraitCatalog
import identitybraces.core.TraitIds
import java.awt.Component
import java.awt.Container
import java.awt.image.BufferedImage
import java.io.File
import javax.imageio.ImageIO
import javax.swing.JComponent

/**
 * The two settings pages, built and painted without a window.
 *
 * A page that throws while being built is a page nobody can open, and the platform reports
 * that as a balloon rather than a test failure. Painting them to a file as well is for
 * looking at: the PNGs land in `build/reports/settings`.
 */
class SettingsPagesTest : BasePlatformTestCase() {
    override fun tearDown() {
        try {
            IdentityBracesSettings.instance.update(IdentityBracesSettings.State())
        } finally {
            super.tearDown()
        }
    }

    fun `test the general page builds, round-trips and paints`() {
        val page = IdentityBracesConfigurable()
        val component = page.createComponent()!!
        page.reset()
        assertFalse(page.isModified)

        paint(component, "general.png", 760, 900)
        page.disposeUIResources()
    }

    fun `test the traits page builds, applies a preset and paints`() {
        val page = TraitsConfigurable()
        val component = page.createComponent()
        page.reset()
        assertFalse(page.isModified)

        paint(component, "traits.png", 1100, 800)

        // Applying through the page must normalise like the settings do: the Unusable preset
        // saturates every shared layer, so nothing may sum past 100 afterwards.
        IdentityBracesSettings.instance.applyPreset("unusable")
        page.reset()
        page.apply()
        val settings = IdentityBracesSettings.instance
        for (layer in listOf(identitybraces.core.TraitLayer.Body, identitybraces.core.TraitLayer.Creature)) {
            val total = TraitCatalog.all.filter { it.layer == layer }.sumOf { settings.traitWeight(it.id) }
            assertTrue("$layer sums to $total", total <= 100)
        }

        assertTrue(settings.traitWeight(TraitIds.CTHULHU) > 0)
        page.disposeUIResources()
    }

    fun `test the colour page lists all thirty-two entries`() {
        val page = IdentityBracesColorSettingsPage()
        assertEquals(32, page.attributeDescriptors.size)
        assertEquals(32, page.additionalHighlightingTagToDescriptorMap!!.size)
        assertTrue(page.demoText.contains("<ib31>"))
    }

    private fun paint(component: JComponent, name: String, width: Int, height: Int) {
        component.setSize(width, height)
        layoutAll(component)
        layoutAll(component)

        val image = BufferedImage(width, height, BufferedImage.TYPE_INT_ARGB)
        val g = image.createGraphics()
        try {
            component.paint(g)
        } finally {
            g.dispose()
        }

        val out = File(System.getProperty("identitybraces.reports") ?: "build/reports/settings", name)
        out.parentFile.mkdirs()
        ImageIO.write(image, "png", out)
    }

    /** Without a window there is no peer to validate against, so lay the tree out by hand. */
    private fun layoutAll(component: Component) {
        component.doLayout()
        if (component is Container) {
            for (child in component.components) {
                layoutAll(child)
            }
        }
    }
}
