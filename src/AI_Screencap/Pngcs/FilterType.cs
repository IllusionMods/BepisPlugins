namespace Pngcs {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Internal PNG predictor filter, or a strategy to select it.
    /// </summary>
    public enum FilterType {
        /// <summary>
        /// No filtering 
        /// </summary>
        FILTER_NONE = 0,
        /// <summary>
        /// SUB filter: uses same row
        /// </summary>
        FILTER_SUB = 1,
        /// <summary>
        ///  UP filter: uses previous row
        /// </summary>
        FILTER_UP = 2,
        /// <summary>
        ///AVERAGE filter: uses neighbors
        /// </summary>
        FILTER_AVERAGE = 3,
        /// <summary>
        /// PAETH predictor
        /// </summary>
        FILTER_PAETH = 4,

        /// <summary>
        /// Default strategy: select one of the standard filters depending on global image parameters
        /// </summary>
        FILTER_DEFAULT = -1, // 


        /// <summary>
        /// Aggressive strategy: select dinamically the filters, trying every 8 rows
        /// </summary>
        FILTER_AGGRESSIVE = -2,

        /// <summary>
        /// Very aggressive and slow strategy: tries all filters for each row
        /// </summary>
        FILTER_VERYAGGRESSIVE = -3,

        /// <summary>
        /// Uses all fiters, one for lines, in cyclic way. Only useful for testing.
        /// </summary>
        FILTER_CYCLIC = -50,

        /// <summary>
        /// Not specified, placeholder for unknown or NA filters. 
        /// </summary>
        FILTER_UNKNOWN = -100
    }


}
