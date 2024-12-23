/* SPDX-FileCopyrightText: 2022 Christian Günther <cgite@gmx.de>
 *
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

namespace Tradez.Sqlite
{
    /// <summary>
    /// Statistics about the save process of some 
    /// FlexStatments
    /// </summary>
    public class SaveResult
    {
        /// <summary>
        /// Statistics about the processed Trades
        /// </summary>
        public Statistics Trades { get; set; }=new Statistics();
        /// <summary>
        /// Statistics about the processed CashPositions
        /// </summary>
        public Statistics Cash { get; set; }= new Statistics();
    }

    public class Statistics
    {
        public Statistics()
        {
            Errors = new string[] { };
        }
        public long New { get; set; } = 0;
        public long Total { get; set; }
        public string[] Errors { get; set; }
    }
}
