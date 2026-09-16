package identitybraces.editor

import com.intellij.openapi.editor.Document
import com.intellij.openapi.util.Key
import com.intellij.openapi.util.UserDataHolderEx
import identitybraces.core.BraceMap
import identitybraces.core.BraceScanner
import identitybraces.settings.IdentityBracesSettings

/**
 * Caches the brace map for the most recent state of one document.
 *
 * Every editor on a document, the hover, the guides and the scene director all derive their
 * answers from this, so they stay in agreement without talking to each other: given the same
 * text and the same settings, [BraceScanner] is a pure function.
 *
 * Keyed on the document's modification stamp and the settings version. Typing produces a
 * new stamp per keystroke and therefore a full rescan; that is a linear pass over the file —
 * about 20 ms per megabyte — which is why `maxFileLength` exists.
 */
class BraceMapCache private constructor() {
    private val gate = Any()

    private var stamp: Long = -1
    private var settingsVersion: Int = -1
    private var map: BraceMap = BraceMap.EMPTY

    /** The map for the document's current text, scanning only if something changed. */
    fun get(document: Document): BraceMap {
        val settings = IdentityBracesSettings.instance
        val currentStamp = document.modificationStamp
        val currentVersion = settings.version

        synchronized(gate) {
            if (stamp == currentStamp && settingsVersion == currentVersion) {
                return map
            }
        }

        // Scan outside the lock. Two threads may briefly duplicate the work; that is cheaper
        // than serialising every reader behind a multi-millisecond scan.
        val fresh = if (!settings.enabled || document.textLength > settings.maxFileLength) {
            BraceMap.EMPTY
        } else {
            BraceMap(BraceScanner.scan(document.immutableCharSequence, settings.toScanSettings()))
        }

        synchronized(gate) {
            stamp = currentStamp
            settingsVersion = currentVersion
            map = fresh
        }

        return fresh
    }

    /** The cached map if it is current, without scanning. Null when stale. */
    fun peek(document: Document): BraceMap? {
        synchronized(gate) {
            return if (stamp == document.modificationStamp && settingsVersion == IdentityBracesSettings.instance.version) map else null
        }
    }

    companion object {
        private val KEY = Key.create<BraceMapCache>("identitybraces.map")

        fun of(document: Document): BraceMapCache {
            return (document as UserDataHolderEx).putUserDataIfAbsent(KEY, BraceMapCache())
        }
    }
}
