/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

//Notice: Fixed version of Language from the Google.Api.Translator project
//-----------------------------------------------------------------------
// <copyright file="Language.cs" company="iron9light">
// Copyright (c) 2010 iron9light
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// </copyright>
// <author>iron9light@gmail.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Aurora.GoogleAPIs
{
    /// <summary>
    ///     Languanges for translate API.
    /// </summary>
    public sealed class Language
    {
        /// <summary>
        ///     The Unknown. Default value.
        /// </summary>
        public static readonly Language Unknown = new Language("Unknown", string.Empty, true);

        /// <summary>
        ///     The Afrikaans.
        /// </summary>
        public static readonly Language Afrikaans = new Language("Afrikaans", "af");

        /// <summary>
        ///     The Albanian.
        /// </summary>
        public static readonly Language Albanian = new Language("Albanian", "sq");

        /// <summary>
        ///     The Amharic.
        /// </summary>
        public static readonly Language Amharic = new Language("Amharic", "am");

        /// <summary>
        ///     The Arabic.
        /// </summary>
        public static readonly Language Arabic = new Language("Arabic", "ar");

        /// <summary>
        ///     The Armenian.
        /// </summary>
        public static readonly Language Armenian = new Language("Armenian", "hy");

        /// <summary>
        ///     The Azerbaijani.
        /// </summary>
        public static readonly Language Azerbaijani = new Language("Azerbaijani", "az");

        /// <summary>
        ///     The Basque.
        /// </summary>
        public static readonly Language Basque = new Language("Basque", "eu");

        /// <summary>
        ///     The Belarusian.
        /// </summary>
        public static readonly Language Belarusian = new Language("Belarusian", "be");

        /// <summary>
        ///     The Bengali.
        /// </summary>
        public static readonly Language Bengali = new Language("Bengali", "bn");

        /// <summary>
        ///     The Bihari.
        /// </summary>
        public static readonly Language Bihari = new Language("Bihari", "bh");

        /// <summary>
        ///     The Bulgarian.
        /// </summary>
        public static readonly Language Bulgarian = new Language("Bulgarian", "bg");

        /// <summary>
        ///     The Burmese.
        /// </summary>
        public static readonly Language Burmese = new Language("Burmese", "my");

        /// <summary>
        ///     The Catalan.
        /// </summary>
        public static readonly Language Catalan = new Language("Catalan", "ca");

        /// <summary>
        ///     The Cherokee.
        /// </summary>
        public static readonly Language Cherokee = new Language("Cherokee", "chr");

        /// <summary>
        ///     The Chinese.
        /// </summary>
        public static readonly Language Chinese = new Language("Chinese", "zh");

        /// <summary>
        ///     The Simplified Chinese.
        /// </summary>
        public static readonly Language ChineseSimplified = new Language("Simplified Chinese", "zh-CN");

        /// <summary>
        ///     The Traditional Chinese.
        /// </summary>
        public static readonly Language ChineseTraditional = new Language("Traditional Chinese", "zh-TW");

        /// <summary>
        ///     The Croatian.
        /// </summary>
        public static readonly Language Croatian = new Language("Croatian", "hr");

        /// <summary>
        ///     The Czech.
        /// </summary>
        public static readonly Language Czech = new Language("Czech", "cs");

        /// <summary>
        ///     The Danish.
        /// </summary>
        public static readonly Language Danish = new Language("Danish", "da");

        /// <summary>
        ///     The Dhivehi.
        /// </summary>
        public static readonly Language Dhivehi = new Language("Dhivehi", "dv");

        /// <summary>
        ///     The Dutch.
        /// </summary>
        public static readonly Language Dutch = new Language("Dutch", "nl");

        /// <summary>
        ///     The English.
        /// </summary>
        public static readonly Language English = new Language("English", "en");

        /// <summary>
        ///     The Esperanto.
        /// </summary>
        public static readonly Language Esperanto = new Language("Esperanto", "eo");

        /// <summary>
        ///     The Estonian.
        /// </summary>
        public static readonly Language Estonian = new Language("Estonian", "et");

        /// <summary>
        ///     The Filipino.
        /// </summary>
        public static readonly Language Filipino = new Language("Filipino", "tl");

        /// <summary>
        ///     The Finnish.
        /// </summary>
        public static readonly Language Finnish = new Language("Finnish", "fi");

        /// <summary>
        ///     The French.
        /// </summary>
        public static readonly Language French = new Language("French", "fr");

        /// <summary>
        ///     The Galician.
        /// </summary>
        public static readonly Language Galician = new Language("Galician", "gl");

        /// <summary>
        ///     The Georgian.
        /// </summary>
        public static readonly Language Georgian = new Language("Georgian", "ka");

        /// <summary>
        ///     The German.
        /// </summary>
        public static readonly Language German = new Language("German", "de");

        /// <summary>
        ///     The Greek.
        /// </summary>
        public static readonly Language Greek = new Language("Greek", "el");

        /// <summary>
        ///     The Guarani.
        /// </summary>
        public static readonly Language Guarani = new Language("Guarani", "gn");

        /// <summary>
        ///     The Gujarati.
        /// </summary>
        public static readonly Language Gujarati = new Language("Gujarati", "gu");

        /// <summary>
        ///     The Hebrew.
        /// </summary>
        public static readonly Language Hebrew = new Language("Hebrew", "iw");

        /// <summary>
        ///     The Hindi.
        /// </summary>
        public static readonly Language Hindi = new Language("Hindi", "hi");

        /// <summary>
        ///     The Hungarian.
        /// </summary>
        public static readonly Language Hungarian = new Language("Hungarian", "hu");

        /// <summary>
        ///     The Icelandic.
        /// </summary>
        public static readonly Language Icelandic = new Language("Icelandic", "is");

        /// <summary>
        ///     The Indonesian.
        /// </summary>
        public static readonly Language Indonesian = new Language("Indonesian", "id");

        /// <summary>
        ///     The Inuktitut.
        /// </summary>
        public static readonly Language Inuktitut = new Language("Inuktitut", "iu");

        /// <summary>
        ///     The Irish.
        /// </summary>
        public static readonly Language Irish = new Language("Irish", "ga");

        /// <summary>
        ///     The Italian.
        /// </summary>
        public static readonly Language Italian = new Language("Italian", "it");

        /// <summary>
        ///     The Japanese.
        /// </summary>
        public static readonly Language Japanese = new Language("Japanese", "ja");

        /// <summary>
        ///     The Kannada.
        /// </summary>
        public static readonly Language Kannada = new Language("Kannada", "kn");

        /// <summary>
        ///     The Kazakh.
        /// </summary>
        public static readonly Language Kazakh = new Language("Kazakh", "kk");

        /// <summary>
        ///     The Khmer.
        /// </summary>
        public static readonly Language Khmer = new Language("Khmer", "km");

        /// <summary>
        ///     The Korean.
        /// </summary>
        public static readonly Language Korean = new Language("Korean", "ko");

        /// <summary>
        ///     The Kurdish.
        /// </summary>
        public static readonly Language Kurdish = new Language("Kurdish", "ku");

        /// <summary>
        ///     The Kyrgyz.
        /// </summary>
        public static readonly Language Kyrgyz = new Language("Kyrgyz", "ky");

        /// <summary>
        ///     The Laothian.
        /// </summary>
        public static readonly Language Laothian = new Language("Laothian", "lo");

        /// <summary>
        ///     The Latvian.
        /// </summary>
        public static readonly Language Latvian = new Language("Latvian", "lv");

        /// <summary>
        ///     The Lithuanian.
        /// </summary>
        public static readonly Language Lithuanian = new Language("Lithuanian", "lt");

        /// <summary>
        ///     The Macedonian.
        /// </summary>
        public static readonly Language Macedonian = new Language("Macedonian", "mk");

        /// <summary>
        ///     The Malay.
        /// </summary>
        public static readonly Language Malay = new Language("Malay", "ms");

        /// <summary>
        ///     The Malayalam.
        /// </summary>
        public static readonly Language Malayalam = new Language("Malayalam", "ml");

        /// <summary>
        ///     The Maltese.
        /// </summary>
        public static readonly Language Maltese = new Language("Maltese", "mt");

        /// <summary>
        ///     The Marathi.
        /// </summary>
        public static readonly Language Marathi = new Language("Marathi", "mr");

        /// <summary>
        ///     The Mongolian.
        /// </summary>
        public static readonly Language Mongolian = new Language("Mongolian", "mn");

        /// <summary>
        ///     The Nepali.
        /// </summary>
        public static readonly Language Nepali = new Language("Nepali", "ne");

        /// <summary>
        ///     The Norwegian.
        /// </summary>
        public static readonly Language Norwegian = new Language("Norwegian", "no");

        /// <summary>
        ///     The Oriya.
        /// </summary>
        public static readonly Language Oriya = new Language("Oriya", "or");

        /// <summary>
        ///     The Pashto.
        /// </summary>
        public static readonly Language Pashto = new Language("Pashto", "ps");

        /// <summary>
        ///     The Persian.
        /// </summary>
        public static readonly Language Persian = new Language("Persian", "fa");

        /// <summary>
        ///     The Polish.
        /// </summary>
        public static readonly Language Polish = new Language("Polish", "pl");

        /// <summary>
        ///     The Portuguese.
        /// </summary>
        public static readonly Language Portuguese = new Language("Portuguese", "pt-PT");

        /// <summary>
        ///     The Punjabi.
        /// </summary>
        public static readonly Language Punjabi = new Language("Punjabi", "pa");

        /// <summary>
        ///     The Romanian.
        /// </summary>
        public static readonly Language Romanian = new Language("Romanian", "ro");

        /// <summary>
        ///     The Russian.
        /// </summary>
        public static readonly Language Russian = new Language("Russian", "ru");

        /// <summary>
        ///     The Sanskrit.
        /// </summary>
        public static readonly Language Sanskrit = new Language("Sanskrit", "sa");

        /// <summary>
        ///     The Serbian.
        /// </summary>
        public static readonly Language Serbian = new Language("Serbian", "sr");

        /// <summary>
        ///     The Sindhi.
        /// </summary>
        public static readonly Language Sindhi = new Language("Sindhi", "sd");

        /// <summary>
        ///     The Sinhalese.
        /// </summary>
        public static readonly Language Sinhalese = new Language("Sinhalese", "si");

        /// <summary>
        ///     The Slovak.
        /// </summary>
        public static readonly Language Slovak = new Language("Slovak", "sk");

        /// <summary>
        ///     The Slovenian.
        /// </summary>
        public static readonly Language Slovenian = new Language("Slovenian", "sl");

        /// <summary>
        ///     The Spanish.
        /// </summary>
        public static readonly Language Spanish = new Language("Spanish", "es");

        /// <summary>
        ///     The Swahili.
        /// </summary>
        public static readonly Language Swahili = new Language("Swahili", "sw");

        /// <summary>
        ///     The Swedish.
        /// </summary>
        public static readonly Language Swedish = new Language("Swedish", "sv");

        /// <summary>
        ///     The Tajik.
        /// </summary>
        public static readonly Language Tajik = new Language("Tajik", "tg");

        /// <summary>
        ///     The Tamil.
        /// </summary>
        public static readonly Language Tamil = new Language("Tamil", "ta");

        /// <summary>
        ///     The Tagalog.
        /// </summary>
        public static readonly Language Tagalog = new Language("Tagalog", "tl");

        /// <summary>
        ///     The Telugu.
        /// </summary>
        public static readonly Language Telugu = new Language("Telugu", "te");

        /// <summary>
        ///     The Thai.
        /// </summary>
        public static readonly Language Thai = new Language("Thai", "th");

        /// <summary>
        ///     The Tibetan.
        /// </summary>
        public static readonly Language Tibetan = new Language("Tibetan", "bo");

        /// <summary>
        ///     The Turkish.
        /// </summary>
        public static readonly Language Turkish = new Language("Turkish", "tr");

        /// <summary>
        ///     The Ukrainian.
        /// </summary>
        public static readonly Language Ukrainian = new Language("Ukrainian", "uk");

        /// <summary>
        ///     The Urdu.
        /// </summary>
        public static readonly Language Urdu = new Language("Urdu", "ur");

        /// <summary>
        ///     The Uzbek.
        /// </summary>
        public static readonly Language Uzbek = new Language("Uzbek", "uz");

        /// <summary>
        ///     The Uighur.
        /// </summary>
        public static readonly Language Uighur = new Language("Uighur", "ug");

        /// <summary>
        ///     The Vietnamese.
        /// </summary>
        public static readonly Language Vietnamese = new Language("Vietnamese", "vi");

        /// <summary>
        ///     The Welsh.
        /// </summary>
        public static readonly Language Welsh = new Language("Welsh", "cy");

        /// <summary>
        ///     The Yiddish.
        /// </summary>
        public static readonly Language Yiddish = new Language("Yiddish", "yi");

        public static readonly ICollection<Language> TranslatableList = new[]
                                                                            {
                                                                                Afrikaans,
                                                                                Albanian,
                                                                                Arabic,
                                                                                Belarusian,
                                                                                Bulgarian,
                                                                                ChineseSimplified,
                                                                                ChineseTraditional,
                                                                                Catalan,
                                                                                Croatian,
                                                                                Czech,
                                                                                Danish,
                                                                                Dutch,
                                                                                English,
                                                                                Estonian,
                                                                                Filipino,
                                                                                Finnish,
                                                                                French,
                                                                                Galician,
                                                                                German,
                                                                                Greek,
                                                                                Hebrew,
                                                                                Hindi,
                                                                                Hungarian,
                                                                                Icelandic,
                                                                                Indonesian,
                                                                                Irish,
                                                                                Italian,
                                                                                Japanese,
                                                                                Korean,
                                                                                Latvian,
                                                                                Lithuanian,
                                                                                Macedonian,
                                                                                Malay,
                                                                                Maltese,
                                                                                Norwegian,
                                                                                Persian,
                                                                                Polish,
                                                                                Portuguese,
                                                                                Romanian,
                                                                                Russian,
                                                                                Spanish,
                                                                                Serbian,
                                                                                Slovak,
                                                                                Slovenian,
                                                                                Swahili,
                                                                                Swedish,
                                                                                Thai,
                                                                                Turkish,
                                                                                Ukrainian,
                                                                                Vietnamese,
                                                                                Welsh,
                                                                                Yiddish,
                                                                            };

        public bool IsDefault;
        public string Name = "";
        public string Value = "";

        private Language(string name, string value)
        {
            Name = name;
            Value = value;
        }

        private Language(string name, string value, bool isDefault)
        {
            IsDefault = isDefault;
            Name = name;
            Value = value;
        }

        public static Language GetValue(string p)
        {
#if (!ISWIN)
            foreach (Language lang in TranslatableList)
            {
                if (lang.Name.StartsWith(p, StringComparison.CurrentCultureIgnoreCase) || lang.Value.StartsWith(p, StringComparison.CurrentCultureIgnoreCase))
                {
                    return lang;
                }
            }
#else
            foreach (
                Language lang in
                    TranslatableList.Where(lang => lang.Name.StartsWith(p, StringComparison.CurrentCultureIgnoreCase)
                                                   ||
                                                   lang.Value.StartsWith(p, StringComparison.CurrentCultureIgnoreCase)))
            {
                return lang;
            }
#endif
            return Unknown;
        }
    }
}