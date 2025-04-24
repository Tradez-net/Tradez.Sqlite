/* SPDX-FileCopyrightText: 2022 Christian Günther <cgite@gmx.de>
 *
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

namespace Tradez.Sqlite
{
    /// <summary>
    /// DTO for a completed trade consisting a opening and a correspondent 
    /// closing trade.
    /// </summary>
    /// <remarks></remarks>
    public class ClosedTrade
    {
        /// <summary>
        /// TradeId of the opening trade
        /// </summary>
        public long OpenTradeId { get; set; } = 0;
        /// <summary>
        /// TradeId of the closing trade
        /// </summary>
        public long CloseTradeId { get; set; } = 0;
        /// <summary>
        /// Quantity of the traded asset
        /// </summary>
        public double Quantity { get; set; } = 0;
    }
}
