/* SPDX-FileCopyrightText: 2022 Christian Günther <cgite@gmx.de>
 *
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using IbFlexReader;
using IbFlexReader.Contracts;
using IbFlexReader.Contracts.Ib;
using System;
using System.Threading.Tasks;

namespace Tradez.Sqlite
{
    /// <summary>
    /// Reading FlexQueries from files or from IB web api.
    /// </summary>
    public static class FlexQuery
    {

        /// <summary>
        /// Getting the FelxStatements from the IB Web-API.
        /// </summary>
        /// <param name="token">Your private IB FlexQuery token.</param>
        /// <param name="queryId">The Id of your FlexQuery</param>
        /// <returns>FlexQuery Statements</returns>
        public static async Task<FlexQueryResponse> FromApiAsync(string token, string queryId)
        {
            FlexResult flexResult = await new Reader().GetByApi(token, queryId);
            return flexResult.FlexQueryResponse;
        }

        /// <summary>
        /// Getting the FelxStatements from the IB Web-API.
        /// </summary>
        /// <param name="token">Your private IB FlexQuery token.</param>
        /// <param name="queryId">The Id of your FlexQuery</param>
        /// <param name="backupFullname">The full path for a backup copy of the FlexQuery xml</param>
        /// <returns>FlexQuery Statements</returns>
        public static async Task<FlexQueryResponse> FromApiAsync(string token, string queryId, string backupFullname)
        {
            FlexResult flexResult = await new Reader().GetByApi(token, queryId, backupFullname);
            return flexResult.FlexQueryResponse;
        }

        /// <summary>
        /// Getting the FelxStatements from the IB Web-API.
        /// </summary>
        /// <param name="token">Your private IB FlexQuery token.</param>
        /// <param name="queryId">The Id of your FlexQuery</param>
        /// <param name="backupFullname">The full path for a backup copy of the FlexQuery xml</param>
        /// <param name="retryCount">Count of retries in case of network failure</param>
        /// <param name="retryDelay">Timespan to wait between the retries</param>
        /// <returns>FlexQuery Statements</returns>
        public static async Task<FlexQueryResponse> FromApiAsync(string token, string queryId, string backupFullname,
            int retryCount, TimeSpan retryDelay)
        {
            FlexResult flexResult = await new Reader().GetByApi(token, queryId, backupFullname, retryCount, retryDelay.Seconds);
            return flexResult.FlexQueryResponse;
        }

        /// <summary>
        /// Getting the FelxStatements from a FleyQuery xml file.
        /// </summary>
        /// <param name="fullname">Full path of the FlexQuery-file</param>
        /// <returns>FlexQuery Statements</returns>
        public static FlexStatements FromFile(string fullname)
        {
            var response = new Reader().
                GetByString(fullname, new Options
                {
                    UseXmlReader = true
                });
            return response.FlexStatements;
        }
    }
}
