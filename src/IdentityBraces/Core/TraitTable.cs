using System.Collections.Generic;

namespace IdentityBraces.Core
{
    /// <summary>
    /// The weight list flattened into the form the per-brace roll actually needs.
    /// </summary>
    /// <remarks>
    /// Built once per scan rather than consulted per brace. The naive version walked all
    /// eighty-five weights once per layer for every brace — on a 240,000-brace file that is
    /// a hundred million iterations, and it pushed a 19&#160;ms scan to 112&#160;ms.
    /// <para>
    /// Only enabled traits appear here, and their bands are pre-summed, so a brace's roll is
    /// a short walk over the handful of traits actually switched on. With the shipped defaults
    /// that is one or two entries per layer.
    /// </para>
    /// </remarks>
    internal sealed class TraitTable
    {
        private struct Band
        {
            public string Id;
            public double Upper;
        }

        private readonly Band[][] _layers;
        private readonly Band[] _effects;

        public TraitTable(IList<TraitWeight> weights)
        {
            _layers = new Band[4][];

            for (int layer = 0; layer < 4; layer++)
            {
                var bands = new List<Band>();
                double cursor = 0;

                if (weights != null)
                {
                    for (int i = 0; i < weights.Count; i++)
                    {
                        TraitWeight weight = weights[i];
                        if ((int)weight.Layer != layer || weight.Percent <= 0)
                        {
                            continue;
                        }

                        cursor += weight.Percent;
                        bands.Add(new Band { Id = weight.Id, Upper = cursor });
                    }
                }

                _layers[layer] = bands.ToArray();
            }

            var effects = new List<Band>();
            if (weights != null)
            {
                for (int i = 0; i < weights.Count; i++)
                {
                    TraitWeight weight = weights[i];
                    if (weight.Layer == TraitLayer.Effect && weight.Percent > 0)
                    {
                        effects.Add(new Band { Id = weight.Id, Upper = weight.Percent });
                    }
                }
            }

            _effects = effects.ToArray();
        }

        public bool IsEmpty
        {
            get
            {
                if (_effects.Length > 0)
                {
                    return false;
                }

                for (int i = 0; i < _layers.Length; i++)
                {
                    if (_layers[i].Length > 0)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>Picks at most one trait from a shared-roll layer.</summary>
        public string PickOne(TraitLayer layer, double roll)
        {
            Band[] bands = _layers[(int)layer];
            for (int i = 0; i < bands.Length; i++)
            {
                if (roll < bands[i].Upper)
                {
                    return bands[i].Id;
                }
            }

            return null;
        }

        public int EffectCount
        {
            get { return _effects.Length; }
        }

        public string EffectAt(int index)
        {
            return _effects[index].Id;
        }

        public double EffectChance(int index)
        {
            return _effects[index].Upper;
        }
    }
}
