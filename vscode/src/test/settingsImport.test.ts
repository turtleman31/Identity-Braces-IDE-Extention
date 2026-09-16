import * as assert from 'node:assert/strict';
import * as fs from 'node:fs';
import * as path from 'node:path';
import { test } from 'node:test';
import { TRAIT_CATALOG } from '../core/traitCatalog';
import { importVisualStudioSettings } from '../settingsImport';

/**
 * Importing the Visual Studio extension's settings.
 *
 * The fixture is a real `settings.ini` shape with every mapped key set to something other
 * than its default, because the failure mode this guards against is a mapping that quietly
 * does nothing — a key read under the wrong name comes back as the default, and a default is
 * exactly what a plausible-looking result is made of.
 */

const FIXTURE = path.join(__dirname, '..', '..', 'test', 'fixtures', 'visualstudio-settings.ini');

function imported() {
    return importVisualStudioSettings(fs.readFileSync(FIXTURE, 'utf8'));
}

test('every scalar setting crosses over', () => {
    const { settings } = imported();

    assert.deepEqual(
        { ...settings, traits: undefined },
        {
            enabled: true,
            curly: true,
            round: false,
            square: true,
            questionUnmatched: false,
            independentBraces: false,
            colorMode: 'depth',
            scopeSpotlight: true,
            spotlightDim: 15,
            complexityWarningDepth: 5,
            indentGuides: true,
            indentGuideStrength: 70,
            enableMotion: false,
            animationFrameRate: 18,
            colorCycleSeconds: 9,
            maxFileLength: 250000,
            decorScale: 0.8,
            thighHighStyle: 'banded',
            tail: false,
            traits: undefined,
        },
    );
});

test('the traits a user turned off stay off', () => {
    // Visual Studio only writes the traits it has turned on and treats the rest as zero. VS
    // Code falls back to the *catalogue* default for anything absent — so an import that
    // copied only what it found would switch `question`, `catgirl`, `cycle` and `thighhighs`
    // back on for someone who had deliberately switched them off.
    const traits = imported().settings.traits as Record<string, number>;

    assert.equal(Object.keys(traits).length, TRAIT_CATALOG.length, 'every trait is written explicitly');
    assert.equal(traits.wizard, 11);
    assert.equal(traits.question, 6);

    for (const trait of TRAIT_CATALOG) {
        const listed = ['question', 'catgirl', 'thighhighs', 'cycle', 'wizard', 'tableflip'].includes(trait.id);
        assert.equal(traits[trait.id] > 0, listed, trait.id);
    }
});

test('what did not come across is reported rather than dropped', () => {
    const { notes } = imported();
    const all = notes.join('\n');

    assert.match(all, /SceneIntervalSeconds/, 'scenes have no counterpart');
    assert.match(all, /ReserveEarSpace/, 'nor does the line transform');
    assert.match(all, /tableflip/, 'and a trait set to 63 that draws nothing is worth saying out loud');

    // Wizard is implemented here, so it must not be in the warning.
    assert.doesNotMatch(all, /wizard/i);
});

test('a missing or malformed file falls back to the defaults rather than throwing', () => {
    const empty = importVisualStudioSettings('');
    assert.equal(empty.settings.enabled, true);
    assert.equal(empty.settings.colorMode, 'palette');
    assert.equal(empty.notes.length, 0);

    const junk = importVisualStudioSettings('# comment only\nnot a pair\nColorMode=banana\n=\n');
    assert.equal(junk.settings.colorMode, 'palette');
    assert.equal(Object.values(junk.settings.traits as Record<string, number>).every((v) => v === 0), true);
});
