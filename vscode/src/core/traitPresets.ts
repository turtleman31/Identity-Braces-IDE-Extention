import { TRAIT_CATALOG } from './traitCatalog';
import { TraitIds } from './traitIds';
import { TraitLayer } from './traitLayer';

export interface TraitPreset {
    readonly id: string;
    readonly name: string;
    readonly description: string;
    readonly weights: Readonly<Record<string, number>>;
}

/**
 * Divides a budget evenly across every trait on one layer.
 *
 * Layers other than Effect share a single 0-100 roll, so their weights have to sum to the
 * budget rather than each being an independent chance. Spreading rather than setting each
 * to the budget is what stops the first trait in the list swallowing every brace.
 */
function layerSpread(layer: TraitLayer, budget: number): Record<string, number> {
    const members = TRAIT_CATALOG.filter((t) => t.layer === layer);
    const weights: Record<string, number> = {};
    if (members.length === 0) {
        return weights;
    }

    const each = Math.max(1, Math.trunc(budget / members.length));
    for (const member of members) {
        weights[member.id] = each;
    }

    return weights;
}

function merge(...parts: Record<string, number>[]): Record<string, number> {
    return Object.assign({}, ...parts);
}

function menagerie(): Record<string, number> {
    const weights = merge(layerSpread(TraitLayer.Creature, 70), layerSpread(TraitLayer.Costume, 55));
    weights[TraitIds.Question] = 5;
    weights[TraitIds.ColourCycle] = 10;
    return weights;
}

function unusable(): Record<string, number> {
    const weights = merge(
        layerSpread(TraitLayer.Creature, 85),
        layerSpread(TraitLayer.Costume, 85),
        layerSpread(TraitLayer.Motion, 85),
        layerSpread(TraitLayer.Body, 45),
    );

    weights[TraitIds.Fire] = 8;
    weights[TraitIds.Tilted] = 25;
    weights[TraitIds.Shadow] = 20;
    weights[TraitIds.GradientFill] = 20;
    weights[TraitIds.BoldItalic] = 20;
    weights[TraitIds.Underline] = 10;
    weights[TraitIds.Distressed] = 12;
    return weights;
}

export const TRAIT_PRESETS: readonly TraitPreset[] = [
    {
        id: 'off',
        name: 'Off',
        description: 'Colours only. Every brace keeps its own glyph.',
        weights: {},
    },
    {
        id: 'default',
        name: 'Default',
        description:
            'What the extension shipped with: a few questions, some colour cycling, the occasional cat.',
        weights: {
            [TraitIds.Question]: 6,
            [TraitIds.ColourCycle]: 8,
            [TraitIds.Catgirl]: 4,
            [TraitIds.ThighHighs]: 4,
        },
    },
    {
        id: 'menagerie',
        name: 'Menagerie',
        description: 'Creatures and costumes across the board, but nothing that moves much.',
        weights: menagerie(),
    },
    {
        id: 'restless',
        name: 'Restless',
        description: 'Everything on the motion layer, spread evenly. Nothing on screen holds still.',
        weights: layerSpread(TraitLayer.Motion, 90),
    },
    {
        id: 'unusable',
        name: 'Unusable',
        description: 'Every layer saturated. This is the setting the extension was designed for.',
        weights: unusable(),
    },
];

export function findPreset(id: string): TraitPreset | undefined {
    return TRAIT_PRESETS.find((p) => p.id === id);
}

/**
 * A preset expressed as the full override map the settings store keeps.
 *
 * Every trait is listed explicitly, including the zeroes: a preset that only wrote its
 * non-zero entries would leave whatever the previous preset had turned on still running.
 */
export function presetWeights(preset: TraitPreset): Record<string, number> {
    const weights: Record<string, number> = {};
    for (const trait of TRAIT_CATALOG) {
        weights[trait.id] = preset.weights[trait.id] ?? 0;
    }

    return weights;
}
