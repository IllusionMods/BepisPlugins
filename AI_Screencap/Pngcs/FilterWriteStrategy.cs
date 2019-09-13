using System;
using System.Collections.Generic;

using System.Text;

namespace Pngcs {
    /// <summary>
    /// Manages the writer strategy for selecting the internal png predictor filter
    /// </summary>
    internal class FilterWriteStrategy {
        private static readonly int COMPUTE_STATS_EVERY_N_LINES = 8;

        private readonly ImageInfo imgInfo;
        private readonly FilterType configuredType; // can be negative (fin dout) 
        private FilterType currentType; // 0-4 
        private int lastRowTested = -1000000;
        private double[] lastSums = new double[5];// performance of each filter (less is better) (can be negative)
        private double[] lastEntropies = new double[5];
        private double[] preference = new double[] { 1.1, 1.1, 1.1, 1.1, 1.2 }; // a priori preference (NONE SUB UP AVERAGE PAETH)
        private int discoverEachLines = -1;
        private double[] histogram1 = new double[256];

        internal FilterWriteStrategy(ImageInfo imgInfo, FilterType configuredType) {
            this.imgInfo = imgInfo;
            this.configuredType = configuredType;
            if (configuredType < 0) { // first guess
                if ((imgInfo.Rows < 8 && imgInfo.Cols < 8) || imgInfo.Indexed
                        || imgInfo.BitDepth < 8)
                    currentType = FilterType.FILTER_NONE;
                else
                    currentType = FilterType.FILTER_PAETH;
            } else {
                currentType = configuredType;
            }
            if (configuredType == FilterType.FILTER_AGGRESSIVE)
                discoverEachLines = COMPUTE_STATS_EVERY_N_LINES;
            if (configuredType == FilterType.FILTER_VERYAGGRESSIVE)
                discoverEachLines = 1;
        }

        internal bool shouldTestAll(int rown) {
            if (discoverEachLines > 0 && lastRowTested + discoverEachLines <= rown) {
                currentType = FilterType.FILTER_UNKNOWN;
                return true;
            } else
                return false;
        }

        internal void setPreference(double none, double sub, double up, double ave, double paeth) {
            preference = new double[] { none, sub, up, ave, paeth };
        }

        internal bool computesStatistics() {
            return (discoverEachLines > 0);
        }

        internal void fillResultsForFilter(int rown, FilterType type, double sum, int[] histo, bool tentative) {
            lastRowTested = rown;
            lastSums[(int)type] = sum;
            if (histo != null) {
                double v, alfa, beta, e;
                alfa = rown == 0 ? 0.0 : 0.3;
                beta = 1 - alfa;
                e = 0.0;
                for (int i = 0; i < 256; i++) {
                    v = ((double)histo[i]) / imgInfo.Cols;
                    v = histogram1[i] * alfa + v * beta;
                    if (tentative)
                        e += v > 0.00000001 ? v * Math.Log(v) : 0.0;
                    else
                        histogram1[i] = v;
                }
                lastEntropies[(int)type] = (-e);
            }
        }

        internal FilterType gimmeFilterType(int rown, bool useEntropy) {
            if (currentType == FilterType.FILTER_UNKNOWN) { // get better
                if (rown == 0)
                    currentType = FilterType.FILTER_SUB;
                else {
                    double bestval = Double.MaxValue;
                    double val;
                    for (int i = 0; i < 5; i++) {
                        val = useEntropy ? lastEntropies[i] : lastSums[i];
                        val /= preference[i];
                        if (val <= bestval) {
                            bestval = val;
                            currentType = (FilterType)i;
                        }
                    }
                }
            }
            if (configuredType == FilterType.FILTER_CYCLIC) {
                currentType = (FilterType)(((int)currentType + 1) % 5);
            }
            return currentType;
        }
    }
}

