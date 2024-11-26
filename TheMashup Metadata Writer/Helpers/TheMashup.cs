using HtmlAgilityPack;
using Newtonsoft.Json;
using Polly;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using TheMashup_Metadata_Writer.Models;

namespace TheMashup_Metadata_Writer.Helpers;
internal static class TheMashup
{
    private static readonly CookieContainer cookieContainer = new();
    private static readonly HttpClientHandler handler = new() { CookieContainer = cookieContainer };
    private static readonly HttpClient client = new(handler);

    public static async Task<string> GetLoginCookieAsync(string email, string password)
    {
        var loginUrl = "https://www.themashup.co.uk/login/validate_credentials";
        var refererUrl = "https://www.themashup.co.uk/login";
        var formData = CreateFormData(
        [
            new KeyValuePair<string, string>("email", email),
            new KeyValuePair<string, string>("password", password),
            new KeyValuePair<string, string>("Submit", "Login")
        ]);

        try
        {
            client.DefaultRequestHeaders.Referrer = new Uri(refererUrl);
            AddDefaultHeaders();

            var response = await client.PostAsync(loginUrl, formData);

            if (!response.IsSuccessStatusCode)
            {
                return "Login failed.";
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(responseContent);
            var warningNode = htmlDoc.DocumentNode.SelectSingleNode("//p[@class='warning']");

            if (warningNode != null)
            {
                return warningNode.InnerText;
            }

            var sessionCookie = cookieContainer.GetCookies(new Uri("https://www.themashup.co.uk"))
                                               .Cast<Cookie>()
                                               .FirstOrDefault(c => c.Name == "tmusession");

            if (sessionCookie != null)
            {
                StoreCookie(sessionCookie, email);
                return "Success";
            }

            return "Login failed. No session cookie found.";
        }
        catch (Exception ex)
        {
            return "Login failed: " + ex.Message;
        }
    }

    private static void AddDefaultHeaders()
    {
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        client.DefaultRequestHeaders.AcceptEncoding.Clear();
        client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("br"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("zstd"));
        client.DefaultRequestHeaders.Host = "www.themashup.co.uk";
        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        client.DefaultRequestHeaders.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("en-GB"));
    }

    public static async Task<bool> CheckSessionValidityAsync()
    {
        var accountUrl = "https://www.themashup.co.uk/site/myaccount";
        try
        {
            EnsureCookieIsSet();
            AddDefaultHeaders();

            var response = await client.GetAsync(accountUrl);

            if (response.StatusCode == HttpStatusCode.OK && response.RequestMessage != null && response.RequestMessage.RequestUri != null && response.RequestMessage.RequestUri.ToString().Contains("login"))
            {
                // Session is invalid - Clear stored cookies and info
                ClearStoredCookie();
                return false;
            }

            // Session is valid
            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            // Log exception
            Console.WriteLine($"Session check failed: {ex.Message}");
            return false;
        }
    }


    private static FormUrlEncodedContent CreateFormData(IEnumerable<KeyValuePair<string, string>> data)
    {
        return new FormUrlEncodedContent(data);
    }

    private static void EnsureCookieIsSet()
    {
        var storedCookie = RetrieveStoredCookie();
        if (storedCookie != null)
        {
            var cookie = new Cookie(storedCookie.Name, storedCookie.Value, storedCookie.Path, storedCookie.Domain)
            {
                Expires = storedCookie.Expires ?? DateTime.MinValue,
                Secure = storedCookie.Secure,
                HttpOnly = storedCookie.HttpOnly,
            };

            var uri = new Uri("https://www.themashup.co.uk");
            var cookies = cookieContainer.GetCookies(uri);

            if (!cookies.Cast<Cookie>().Any(c => c.Name == storedCookie.Name))
            {
                cookieContainer.Add(uri, cookie);
            }
            else
            {
                // Update existing cookie if necessary
                var existingCookie = cookies[storedCookie.Name];
                existingCookie.Value = storedCookie.Value;
                existingCookie.Path = storedCookie.Path;
                existingCookie.Domain = storedCookie.Domain;
                existingCookie.Expires = storedCookie.Expires ?? DateTime.MinValue;
                existingCookie.Secure = storedCookie.Secure;
                existingCookie.HttpOnly = storedCookie.HttpOnly;
            }
        }
    }

    private static void UpdateCookies(HttpResponseMessage response)
    {
        var uri = new Uri("https://www.themashup.co.uk");
        var sessionCookie = cookieContainer.GetCookies(uri)
                                           .Cast<Cookie>()
                                           .FirstOrDefault(c => c.Name == "tmusession");

        if (sessionCookie != null)
        {
            var storedCookie = RetrieveStoredCookie();

            if (storedCookie == null || storedCookie.Value != sessionCookie.Value)
            {
                StoreCookie(sessionCookie, RetrieveStoredEmail());

                if (storedCookie != null)
                {
                    Console.WriteLine($"Cookie tmusession updated. Old value: {storedCookie.Value} New value: {sessionCookie.Value}");
                }
            }
        }
    }

    public static async Task<List<SongModel>> SearchSongAsync(string artist, string song, CancellationToken cancellationToken)
    {
        var searchUrl = "https://www.themashup.co.uk/site/search";
        var songData = new List<SongModel>();
        
        if (null == RetrieveStoredCookie())
        {
            return [];
        }

        EnsureCookieIsSet();
        AddDefaultHeaders();

        var policy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(10, retryAttempt => TimeSpan.FromSeconds(retryAttempt));

        var songClean = song;
        var artistClean = artist;

        if (song.Contains('\'') || song.Contains('`') || song.Contains('´') || song.Contains('’'))
        {
            while (songClean.Contains('\'') || songClean.Contains('`') || songClean.Contains('´') || songClean.Contains('’'))
            {
                if(songClean.Contains('\''))
                {
                    var badApo = songClean.IndexOf('\'');
                    var firstSpace = songClean.IndexOf(' ', badApo);
                    var cleanupString = songClean.Substring(badApo, firstSpace - badApo);

                    songClean = songClean.Replace(cleanupString, null);
                }

                if(songClean.Contains('`'))
                {
                    var badApo = songClean.IndexOf('`');
                    var firstSpace = songClean.IndexOf(' ', badApo);
                    var cleanupString = songClean.Substring(badApo, firstSpace - badApo);

                    songClean = songClean.Replace(cleanupString, null);
                }

                if(songClean.Contains('´'))
                {
                    var badApo = songClean.IndexOf('´');
                    var firstSpace = songClean.IndexOf(' ', badApo);
                    var cleanupString = songClean.Substring(badApo, firstSpace - badApo);

                    songClean = songClean.Replace(cleanupString, null);
                }

                if(songClean.Contains('’'))
                {
                    var badApo = songClean.IndexOf('’');
                    var firstSpace = songClean.IndexOf(' ', badApo);
                    var cleanupString = songClean.Substring(badApo, firstSpace - badApo);

                    songClean = songClean.Replace(cleanupString, null);
                }
            }
        }

        for(var i = 0; i<2; i++)
        {
            try
            {
                FormUrlEncodedContent formData;
                if (i == 0)
                {
                    
                    formData = CreateFormData(
                    [
                        new KeyValuePair<string, string>("advsearch", "0"),
                        new KeyValuePair<string, string>("searchbutt", "Search"),
                        new KeyValuePair<string, string>("searchtrackname", songClean),
                        new KeyValuePair<string, string>("searchtrackartist", artist)
                    ]);
                }
                else
                {
                    // No hits on songs using exact search - Trying approximate search
                    formData = CreateFormData(
                    [
                        new KeyValuePair<string, string>("advsearch", "0"),
                        new KeyValuePair<string, string>("searchbutt", "Search"),
                        new KeyValuePair<string, string>("searchtrackname", songClean.Substring(0, songClean.IndexOf('(')).Trim()),
                        new KeyValuePair<string, string>("searchtrackartist", artist)
                    ]);
                }

                var response = await policy.ExecuteAsync(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await client.PostAsync(searchUrl, formData);
                });

                // Update cookies if needed
                UpdateCookies(response);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    songData = ParseSearchResults(responseContent);
                    if(songData.Count > 0)
                    {
                        return songData;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return [];
            }
            catch (Exception ex)
            {
                // Log exception
                Console.WriteLine($"Search failed: {ex.Message}");
            }
        }

        return [];
    }

    private static List<SongModel> ParseSearchResults(string html)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);
        var songs = new List<SongModel>();

        // Fetch all notes that are downloaded
        var songNodes = htmlDoc.DocumentNode.SelectNodes("//tbody[@id='playlist']//tr[@class='downloaded']");

        if (songNodes == null)
        {
            // No hits on downloaded, let's check the other nodes "yougood"
            songNodes = htmlDoc.DocumentNode.SelectNodes("//tbody[@id='playlist']//tr[@class='yougood']");
            
            // Nothing - Likely it's been removed from the site
            if(songNodes == null)
            {
                return songs;
            }
        }

        foreach (var node in songNodes)
        {
            var song = new SongModel();
            var titleNode = node.SelectSingleNode(".//td[contains(@class, 'tab_name')]");

            if (titleNode != null)
            {
                song.Title = titleNode.InnerHtml.Split("<br>")[0].Trim();
                song.Artist = titleNode.GetAttributeValue("data-artist", string.Empty);

                var genreNodes = titleNode.SelectNodes(".//a[contains(@href, '/site/genre/')]");
                if (genreNodes != null)
                {
                    song.Genre = genreNodes.Select(link => link.InnerText.Trim()).ToArray();
                }

                var classificationNodes = titleNode.SelectNodes(".//a[contains(@href, '/site/filtertype/')]");
                if (classificationNodes != null)
                {
                    song.Classifications = classificationNodes.Select(link => link.InnerText.Trim()).ToArray();
                }

                var mashupClassificationNodes = titleNode.SelectNodes(".//span[contains(@class, 'tmupill pillsc1')]");
                if (mashupClassificationNodes != null)
                {
                    song.TheMashupClassification = mashupClassificationNodes
                        .Select(link => link.InnerText.Trim().Replace("Weekend Sessentials ", ""))
                        .ToArray();
                }
            }

            var bpmNode = node.SelectSingleNode(".//td[contains(@class, 'tab_bpm')]");

            if (bpmNode != null)
            {
                var bpm = bpmNode.InnerText.Trim();

                if (bpm.Contains('-'))
                {
                    bpm = bpm.Split('-')[0];
                }

                if (bpm.Contains('.'))
                {
                    bpm = Math.Round(Convert.ToDouble(bpm, CultureInfo.InvariantCulture) * 2).ToString();
                }

                song.BPM = int.Parse(bpm);
            }

            var ratingNode = node.SelectSingleNode(".//div[@class='ratingaverage']");

            if (ratingNode != null)
            {
                song.Rating = double.Parse(ratingNode.InnerText.Trim(), CultureInfo.InvariantCulture);
            }

            var dateNode = node.SelectSingleNode(".//td[contains(@class, 'tab_date')]");

            if (dateNode != null)
            {
                var dateValue = dateNode.GetAttributeValue("data-order", string.Empty);
                if (!string.IsNullOrEmpty(dateValue))
                {
                    song.Uploaded = DateTime.Parse(dateValue.Trim(), CultureInfo.InvariantCulture);
                }
            }

            var idNode = node.GetAttributeValue("data-id", string.Empty);
            if (!string.IsNullOrEmpty(idNode))
            {
                song.Id = int.Parse(idNode.Trim());
            }

            songs.Add(song);
        }

        return songs;
    }

    public static bool IsLoggedIn()
    {
        if(RetrieveStoredCookie() != null)
        {
            return true;
        }
        return false;
    }

    private static void StoreCookie(Cookie cookie, string email)
    {
        var serializableCookie = new SerializableCookie
        {
            Name = cookie.Name,
            Value = cookie.Value,
            Path = cookie.Path,
            Domain = cookie.Domain,
            Expires = cookie.Expires != DateTime.MinValue ? cookie.Expires : null,
            Secure = cookie.Secure,
            HttpOnly = cookie.HttpOnly,
        };

        var cookieJson = JsonConvert.SerializeObject(serializableCookie);
        Preferences.Set("tmusession", cookieJson);
        Preferences.Set("email", email);
    }


    public static void ClearStoredCookie()
    {
        Preferences.Remove("tmusession");
        Preferences.Remove("email");
    }

    public static SerializableCookie? RetrieveStoredCookie()
    {
        var cookieJson = Preferences.Get("tmusession", "");
        try
        {
            if (!string.IsNullOrEmpty(cookieJson))
            {
                return JsonConvert.DeserializeObject<SerializableCookie>(cookieJson);
            }
        }
        catch
        {
            // Invalid preferences data. Nuke the preferences
            Preferences.Remove("tmusession");
            Preferences.Remove("email");
        }

        return null;
    }


    public static string RetrieveStoredEmail()
    {
        return Preferences.Get("email", "");
    }
}
