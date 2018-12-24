using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace BandcampDownloader {

    internal static class BandcampHelper {

        /// <summary>
        /// Retrieves the data on the album of the specified Bandcamp page.
        /// </summary>
        /// <param name="htmlCode">The HTML source code of a Bandcamp album page.</param>
        /// <returns>The data on the album of the specified Bandcamp page.</returns>
        public static Album GetAlbum(String htmlCode) {
            // Keep the interesting part of htmlCode only
            String albumData;
            try {
                albumData = GetAlbumData(htmlCode);
            } catch (Exception e) {
                throw new Exception("Could not retrieve album data in HTML code.", e);
            }

            // Fix some wrongly formatted JSON in source code
            albumData = FixJson(albumData);

            // Deserialize JSON
            Album album;
            try {
                var settings = new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };
                album = JsonConvert.DeserializeObject<JsonAlbum>(albumData, settings).ToAlbum();
            } catch (Exception e) {
                throw new Exception("Could not deserialize JSON data.", e);
            }

            // Extract lyrics from album page
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlCode);
            foreach (Track track in album.Tracks) {
                HtmlNode lyricsElement = htmlDoc.GetElementbyId("_lyrics_" + track.Number);
                if (lyricsElement != null) {
                    track.Lyrics = lyricsElement.InnerText.Trim();
                }
            }

            return album;
        }
        private static readonly Regex BandUrlRegex = new Regex("band_url = \"(?<url>.*)\"", RegexOptions.Compiled);
        private static readonly Regex AlbumTrackRegex = new Regex("href=\"(?<url>/(album|track)/.*)\"", RegexOptions.Compiled);

        /// <summary>
        /// Retrieves all the albums URL existing on the specified Bandcamp page.
        /// </summary>
        /// <param name="htmlCode">The HTML source code of a Bandcamp page.</param>
        /// <returns>The albums URL existing on the specified Bandcamp page.</returns>
        public static List<String> GetAlbumsUrl(String htmlCode) {
            Match artistPageMatch = BandUrlRegex.Match(htmlCode);
            // Get artist bandcamp page
            if (!artistPageMatch.Success) {
                throw new NoAlbumFoundException();
            }
            String artistPage = artistPageMatch.Groups["url"].Value;

            // Get albums ("real" albums or track-only pages) relative urls
            MatchCollection albumMatches = AlbumTrackRegex.Matches(htmlCode);
            if (albumMatches.Count == 0) {
                throw new NoAlbumFoundException();
            }
            
            var albumsUrl = albumMatches.Cast<Match>().Select(m => artistPage + m.Groups["url"].Value).Distinct().ToList();
            return albumsUrl;
        }

        private static readonly Regex FixJsonRegex = new Regex("(?<root>url: \".+)\" \\+ \"(?<album>.+\",)", RegexOptions.Compiled);

        private static String FixJson(String albumData) {
            // Some JSON is not correctly formatted in bandcamp pages, so it needs to be fixed before we can deserialize it

            // In trackinfo property, we have for instance:
            //     url: "http://verbalclick.bandcamp.com" + "/album/404"
            // -> Remove the " + "
            String fixedData = FixJsonRegex.Replace(albumData, "${root}${album}");

            return fixedData;
        }

        private static readonly Regex AlbumDataRegex = new Regex("var TralbumData = ({.*});", RegexOptions.Compiled);

        private static String GetAlbumData(String htmlCode) {
            Match regexMatch = AlbumDataRegex.Match(htmlCode);
 
            if (regexMatch.Success) return regexMatch.Groups[0].Value;
            // Could not find startString
            Exception up = new Exception("Could not find the following string in HTML code: var TralbumData = {");
            throw up;
        }
    }
}