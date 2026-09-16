import { TraitLayer } from './traitLayer';

/** One trait's identity and how often it should come up. */
export interface TraitWeight {
    readonly id: string;
    readonly layer: TraitLayer;
    readonly percent: number;
}

interface Band {
    readonly id: string;
    readonly upper: number;
}

/**
 * The weight list flattened into the form the per-brace roll actually needs.
 *
 * Built once per scan rather than consulted per brace. The naive version walks all
 * eighty-six weights once per layer for every brace — on a 240,000-brace file that is a
 * hundred million iterations, and in the C# original it pushed a 19&nbsp;ms scan to 112&nbsp;ms.
 *
 * Only enabled traits appear here, and their bands are pre-summed, so a brace's roll is a
 * short walk over the handful of traits actually switched on. With the shipped defaults
 * that is one or two entries per layer.
 */
export class TraitTable {
    private readonly layers: Band[][];
    private readonly effects: Band[];

    constructor(weights: readonly TraitWeight[] | undefined) {
        this.layers = [[], [], [], []];

        for (let layer = 0; layer < 4; layer++) {
            let cursor = 0;
            for (const weight of weights ?? []) {
                if ((weight.layer as number) !== layer || weight.percent <= 0) {
                    continue;
                }

                cursor += weight.percent;
                this.layers[layer].push({ id: weight.id, upper: cursor });
            }
        }

        this.effects = [];
        for (const weight of weights ?? []) {
            if (weight.layer === TraitLayer.Effect && weight.percent > 0) {
                this.effects.push({ id: weight.id, upper: weight.percent });
            }
        }
    }

    get isEmpty(): boolean {
        return this.effects.length === 0 && this.layers.every((bands) => bands.length === 0);
    }

    /** Picks at most one trait from a shared-roll layer. */
    pickOne(layer: TraitLayer, roll: number): string | null {
        const bands = this.layers[layer as number];
        for (const band of bands) {
            if (roll < band.upper) {
                return band.id;
            }
        }

        return null;
    }

    get effectCount(): number {
        return this.effects.length;
    }

    effectAt(index: number): string {
        return this.effects[index].id;
    }

    effectChance(index: number): number {
        return this.effects[index].upper;
    }
}
