/* SPDX-FileCopyrightText: 2022 Christian Günther <cgite@gmx.de>
 *
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

namespace Tradez.Sqlite.Tests
{
    public class AnonymizerTests
    {
        /// <summary>
        /// Not realy a test, but a small function to anonmize some
        /// xml-files
        /// </summary>
        [Test]
        public void Can_Anonymize_Xml()
        {
            string originalXml = File.ReadAllText(@"..\..\..\testfiles\kmi.xml");
            string anonymizedXml = FlexQueryAnonymizer.AnonymizeXml(originalXml);
            File.WriteAllText(@"..\..\..\testfiles\testfile1.xml", anonymizedXml);
        }

        
    }
}