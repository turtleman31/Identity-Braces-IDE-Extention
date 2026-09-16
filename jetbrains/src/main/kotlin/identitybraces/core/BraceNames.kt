package identitybraces.core

/**
 * Gives a brace a name, derived from its identity.
 *
 * Stable for the life of the brace, and stable across machines and editors, because it is a
 * pure function of the same identity hash that decides the brace's colour and traits. Two
 * people looking at the same file agree about which one is Reginald.
 *
 * Built from three independent draws rather than one list of names, so the catalogue is a
 * few dozen words instead of thousands and still rarely repeats: with 24 titles, 32 given
 * names and 24 epithets there are around eighteen thousand combinations.
 */
object BraceNames {
    private const val TITLE_SALT: ULong = 0x717DE5A17uL
    private const val GIVEN_SALT: ULong = 0x91BE0DA17uL
    private const val EPITHET_SALT: ULong = 0xED17BE7FuL
    private const val SHAPE_SALT: ULong = 0x5AAFE5A17uL

    private val titles = arrayOf(
        "Sir", "Dame", "Captain", "Doctor", "Professor", "Admiral", "Baron", "Duchess",
        "Chief", "Judge", "Marshal", "Bishop", "Colonel", "Commodore", "Vicar", "Warden",
        "Lord", "Lady", "Sergeant", "Inspector", "Abbot", "Regent", "Consul", "Steward",
    )

    private val given = arrayOf(
        "Reginald", "Beatrice", "Mortimer", "Winifred", "Cuthbert", "Prudence", "Barnaby",
        "Millicent", "Horace", "Agatha", "Percival", "Edwina", "Clarence", "Hyacinth",
        "Ambrose", "Theodora", "Gideon", "Rosalind", "Alistair", "Cordelia", "Bartholomew",
        "Philippa", "Montgomery", "Griselda", "Rupert", "Josephine", "Egbert", "Marigold",
        "Fitzwilliam", "Henrietta", "Archibald", "Wilhelmina",
    )

    private val epithets = arrayOf(
        "the Unclosed", "the Patient", "the Unmatched", "of the Third Nesting",
        "the Indented", "the Verbose", "the Deprecated", "the Refactored", "the Unreachable",
        "the Well-Formed", "the Dangling", "the Terse", "the Overloaded", "the Immutable",
        "the Recursive", "the Deeply Nested", "the Uncommented", "the Legacy",
        "the Escaped", "the Trailing", "the Orphaned", "the Idempotent", "the Volatile",
        "the Considered Harmful",
    )

    /**
     * This brace's name.
     *
     * Three shapes, so a screenful is not a wall of identically-structured names: some braces
     * get a title, some an epithet, some both, some just a name. The shape is drawn from the
     * identity too, so it is as stable as the rest of it.
     */
    fun of(identity: ULong): String {
        val title = pick(titles, identity, TITLE_SALT)
        val name = pick(given, identity, GIVEN_SALT)
        val epithet = pick(epithets, identity, EPITHET_SALT)

        return when ((Hash.toUnitInterval(Hash.mix(identity, SHAPE_SALT)) * 4).toInt() % 4) {
            0 -> name
            1 -> "$title $name"
            2 -> "$name $epithet"
            else -> "$title $name $epithet"
        }
    }

    private fun pick(from: Array<String>, identity: ULong, salt: ULong): String {
        return from[Hash.toIndex(Hash.mix(identity, salt), from.size)]
    }
}
