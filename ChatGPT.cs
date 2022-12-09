using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Security.Policy;
using System.Text;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Filters;
using ServiceStack;
using DibbrBot;
using Twilio.Jwt.AccessToken;
using System.Net;
using static ChatGPT3.ChatGPT;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Linq;
using ServiceStack.Host;

namespace ChatGPT3;

public class ChatGPT
{
    public CookieContainer cookies = new CookieContainer();
    public string cookie = "intercom-device-id-dgkjq2bp=9ab266e7-104f-46df-a8ec-2726d512e855; __Host-next-auth.csrf-token=16594dd4956288954395ba48c00687e3641cc9e09a5e1483efcadcb0d254168e%7C3647ca9d35790a9d43b74f29a045c2fbbe749691582d7319289c55bc2dc02efb; ajs_user_id=nfC9zQAtFYO2fZHxuR2LN67NFtT2; ajs_anonymous_id=2a42d869-61bc-4339-9fdc-b4c6dde89a40; intercom-session-dgkjq2bp=UkZ2RjJkR051alppaWpJdHdETGdxZk5iQjFaRzFVd3NMRWVLV3AvT29zWHAzc2t3RHJmLzJRZGtGejlYODUwTy0tenhRYWtDRmlTOXpEWkNkOElNZmEyUT09--49ed3fda59cb3aa541ad13002d71283c78b8cd52; __Secure-next-auth.callback-url=https%3A%2F%2Fchat.openai.com%2Fchat; mp_d7d7628de9d5e6160010b84db960a7ee_mixpanel=%7B%22distinct_id%22%3A%20%22user-Dl4ifhTmjdWafGAXNhyWPGpr%22%2C%22%24device_id%22%3A%20%2217d39fbf4095ed-0ad3e62e84e519-978183a-144000-17d39fbf40a65e%22%2C%22%24search_engine%22%3A%20%22google%22%2C%22%24initial_referrer%22%3A%20%22https%3A%2F%2Fwww.google.com%2F%22%2C%22%24initial_referring_domain%22%3A%20%22www.google.com%22%2C%22%24user_id%22%3A%20%22user-Dl4ifhTmjdWafGAXNhyWPGpr%22%7D; __Secure-next-auth.session-token=eyJhbGciOiJkaXIiLCJlbmMiOiJBMjU2R0NNIn0..taTydEf9_b4-09nb.Pm929co2VQzWZ-jmEjiSbyf2Rn2Y50QILr64Fliubhva1rqvSP_gbvumalq-nNkE6yEzqDxoJkSevMcQ-Gqh4Azha8bNMeHjbiJcV5o0D0TLz1ZDF4C7VxxfFKJkX8XrxkBZxF4GtKYWeWrirxyk18R1l9hdhICVU0rNeBKlVLGCH3ZlOUb7o4JVqE9UDiu-jJMwr29CSWv0lH_go9X6l7kiADTC-mHA847jz4bNYrbrGzqgKpkaZmnRb0C0WTxn4yh64s0ne0svEv2ZQDSYF7JLRGUhFMK1ekWcpV-HkZPer9vdSM_aJ3KeMfkNWpubr1b-n_xkobZH1CdMPW2yG-_kXTaJP_nLVse_WE4AH-tgs6A100o4Il1ZMir3gtdwjY6xCOOXBlde_P97bvNCnFbW7CAW0XnoPhXL34kRijchS3LAOcoWYzWBkE_LN8moG4Mvw70SccB8ZPoNdxUG3ZfcULQy9IFLJ5dZsThXf2SP6hMlYWQdKZr2FvG9spWIaQG4ELehTZ6M1dsr2VsNKmAIDsj22CgH_N5lvN-janA5YL9JrR9oEVX5lbZHU76sB3DhSBYHey2iGydPZg_-Un6ch6GnWgH0h3XiTiqz1Gkl0VEWv90_SuqsEI0INxCQoTjnnFh0eG3VdaWpmtL5feH3G9m7JI8-7xCHzB-CFrTUxazH_aLTidGJTb5H4gwasQmyooUJp58DihLaGQZBFq7LAWD-VnwtWmxh2o4WIgtmgq2YSrfJ4Uo_q_eWMQqvCykbHiz3VGLQ8FJDPLGQWhNy95k0TNVUPe6jTYT5TdHkPbt64ZiwF472TOLWbABQWKwxbUIGTnnIAvQh7ljZ2ahf-efwG12sm-r72ANYbZe0E-WvIs921960vtE_OYxZdPl_yyxTFUFah9VdseNiiLQhHOJMhZ9nJjn6IxVXNMcYLNgJ55iUpKur8x15GX-gnbXFtsd-bPONZwpK3mMwO1ZTOGRyG1nn-wK6hof3FOvQfMWp-MEyWki366VAI221f3J-VGZ8LV3B-jaYm0OZnggRe32DLyaHZSxB2hQUBhb_FJkFXScbClm56XuPdfx_csqoBTKvy3-ac3NUrIAAEUM7tw3lXYoQ92ufiQDewottbjr3ZVtty_LiwkyiQk-rsfgwgDJg2Bp37PRmi6caYKXZ6XG869v3x3oZRh_oi7PDW4gzq79CYQYFmZRUMKCK8QdAHrAhLj-FcI7sejRChVth4hcT6HAGiItmi108KK1tI1zLKt2f3TltKDLocQMizNFG1NT4Lr17wFXZdwm7czCOyI_g3wfh-22CnqFSBTMrH6H4KYyW3OJhOeyr0-x_2XLM7bKA3ygVR-Ys6mf0fVSwzU4Ata6l0JuKqIS5LDgZ3z9-qZ6guw3Xq7U-Qdxm0wQHvRYG6hH7GPnGYOlItRA-Y2Vm3XctjABVBlXLC5DFFfSwZsTLYA_HOmNveXJEM-8AIBnht5fmPwky_rekTu-km1eH3aRCHTwYIcb5EnVlfcwxU9ZLeBufZewHtuDAppqkRr4TYn25VqnCiwVq5Mkj_KSHj_CDLTOq4kEci6utHexHYq45JtmMNqL7UfbGa6591xuL6NV8fNSriIvIxk5ThEkyDUTtefIgq_byAphLh_X3AOpIOs1zULw3qo9Zr9kQsJB9lfxYROKgXcybRFihIQIfBHm1FJZOp4E5-z9WVbh6WAy6SKNqaRJfl8dtPwpncrE6fBFidd3apColkvmg-Qa532U6uCIgNR7OLoOrsThCGCxy8SEaEvtrjpHRRx1oQUc1ZnWMOxr23cdNuif3xwhUYdLuD-r1KWE1P5pP1zZMNQayyL6AR9xtubGptjz1SvLRyxGE0IKqkyMY1sEr6xGJeeX1q5RKtjEK6rizeLI0jNPT-HUti010fpEgFayBgZzJ-sM4iBnyrLCIqaV5QW7xOQqH4btAiTm7i9BRHm2aRBxEB9WzQ8jfUt9MGC7PtZ3mjbgb0T8KoElVwjQzQU2bdDUc4RY9zVKyUpYtepuzfhvvk3qSDbimCu-5R3k9KOgxR4JnnivfFY0AvDI6pGWIFzOTaqZKb56kGCXs3PkpZpc4JHOjj2HHV3ENQ8WoSG1BBgyabZKwG4nXUisdkXV8LgzCdKqGRyQScfnVuun356bEGkzBex791ckmZxr1VEVBGTMntCq2zCu7J1gYK5PU7hNDPTdHeMa565p9CNS1CxkHzqmuJqSi5UmXewcGGWjtc_4cmM1lPUrve0859QDArpWiLBtmUzYC_vta4l8wfe_4k7UZup5XgX_dfU41eXvqk755IN8uSPacc_1h.6jHOjQgdSd0Cmeb9ztkegA";   
    public Session session { get; set; }
    public string sessionToken = "eyJhbGciOiJkaXIiLCJlbmMiOiJBMjU2R0NNIn0..taTydEf9_b4-09nb.Pm929co2VQzWZ-jmEjiSbyf2Rn2Y50QILr64Fliubhva1rqvSP_gbvumalq-nNkE6yEzqDxoJkSevMcQ-Gqh4Azha8bNMeHjbiJcV5o0D0TLz1ZDF4C7VxxfFKJkX8XrxkBZxF4GtKYWeWrirxyk18R1l9hdhICVU0rNeBKlVLGCH3ZlOUb7o4JVqE9UDiu-jJMwr29CSWv0lH_go9X6l7kiADTC-mHA847jz4bNYrbrGzqgKpkaZmnRb0C0WTxn4yh64s0ne0svEv2ZQDSYF7JLRGUhFMK1ekWcpV-HkZPer9vdSM_aJ3KeMfkNWpubr1b-n_xkobZH1CdMPW2yG-_kXTaJP_nLVse_WE4AH-tgs6A100o4Il1ZMir3gtdwjY6xCOOXBlde_P97bvNCnFbW7CAW0XnoPhXL34kRijchS3LAOcoWYzWBkE_LN8moG4Mvw70SccB8ZPoNdxUG3ZfcULQy9IFLJ5dZsThXf2SP6hMlYWQdKZr2FvG9spWIaQG4ELehTZ6M1dsr2VsNKmAIDsj22CgH_N5lvN-janA5YL9JrR9oEVX5lbZHU76sB3DhSBYHey2iGydPZg_-Un6ch6GnWgH0h3XiTiqz1Gkl0VEWv90_SuqsEI0INxCQoTjnnFh0eG3VdaWpmtL5feH3G9m7JI8-7xCHzB-CFrTUxazH_aLTidGJTb5H4gwasQmyooUJp58DihLaGQZBFq7LAWD-VnwtWmxh2o4WIgtmgq2YSrfJ4Uo_q_eWMQqvCykbHiz3VGLQ8FJDPLGQWhNy95k0TNVUPe6jTYT5TdHkPbt64ZiwF472TOLWbABQWKwxbUIGTnnIAvQh7ljZ2ahf-efwG12sm-r72ANYbZe0E-WvIs921960vtE_OYxZdPl_yyxTFUFah9VdseNiiLQhHOJMhZ9nJjn6IxVXNMcYLNgJ55iUpKur8x15GX-gnbXFtsd-bPONZwpK3mMwO1ZTOGRyG1nn-wK6hof3FOvQfMWp-MEyWki366VAI221f3J-VGZ8LV3B-jaYm0OZnggRe32DLyaHZSxB2hQUBhb_FJkFXScbClm56XuPdfx_csqoBTKvy3-ac3NUrIAAEUM7tw3lXYoQ92ufiQDewottbjr3ZVtty_LiwkyiQk-rsfgwgDJg2Bp37PRmi6caYKXZ6XG869v3x3oZRh_oi7PDW4gzq79CYQYFmZRUMKCK8QdAHrAhLj-FcI7sejRChVth4hcT6HAGiItmi108KK1tI1zLKt2f3TltKDLocQMizNFG1NT4Lr17wFXZdwm7czCOyI_g3wfh-22CnqFSBTMrH6H4KYyW3OJhOeyr0-x_2XLM7bKA3ygVR-Ys6mf0fVSwzU4Ata6l0JuKqIS5LDgZ3z9-qZ6guw3Xq7U-Qdxm0wQHvRYG6hH7GPnGYOlItRA-Y2Vm3XctjABVBlXLC5DFFfSwZsTLYA_HOmNveXJEM-8AIBnht5fmPwky_rekTu-km1eH3aRCHTwYIcb5EnVlfcwxU9ZLeBufZewHtuDAppqkRr4TYn25VqnCiwVq5Mkj_KSHj_CDLTOq4kEci6utHexHYq45JtmMNqL7UfbGa6591xuL6NV8fNSriIvIxk5ThEkyDUTtefIgq_byAphLh_X3AOpIOs1zULw3qo9Zr9kQsJB9lfxYROKgXcybRFihIQIfBHm1FJZOp4E5-z9WVbh6WAy6SKNqaRJfl8dtPwpncrE6fBFidd3apColkvmg-Qa532U6uCIgNR7OLoOrsThCGCxy8SEaEvtrjpHRRx1oQUc1ZnWMOxr23cdNuif3xwhUYdLuD-r1KWE1P5pP1zZMNQayyL6AR9xtubGptjz1SvLRyxGE0IKqkyMY1sEr6xGJeeX1q5RKtjEK6rizeLI0jNPT-HUti010fpEgFayBgZzJ-sM4iBnyrLCIqaV5QW7xOQqH4btAiTm7i9BRHm2aRBxEB9WzQ8jfUt9MGC7PtZ3mjbgb0T8KoElVwjQzQU2bdDUc4RY9zVKyUpYtepuzfhvvk3qSDbimCu-5R3k9KOgxR4JnnivfFY0AvDI6pGWIFzOTaqZKb56kGCXs3PkpZpc4JHOjj2HHV3ENQ8WoSG1BBgyabZKwG4nXUisdkXV8LgzCdKqGRyQScfnVuun356bEGkzBex791ckmZxr1VEVBGTMntCq2zCu7J1gYK5PU7hNDPTdHeMa565p9CNS1CxkHzqmuJqSi5UmXewcGGWjtc_4cmM1lPUrve0859QDArpWiLBtmUzYC_vta4l8wfe_4k7UZup5XgX_dfU41eXvqk755IN8uSPacc_1h.6jHOjQgdSd0Cmeb9ztkegA";
    ResponseMessage lastResponse;
    public string url = "https://chat.openai.com/backend-api/conversation";

    public ChatGPT()
    {
        cookies.SetCookies(new Uri("http://chat.openai.com"), "__Secure-next-auth.session-token=eyJhbGciOiJkaXIiLCJlbmMiOiJBMjU2R0NNIn0..3zLYEQO51yeRQq4p.VsOCJ9FBm-5Nb3FoW7Wdiq4s5u29Qx7ysLNrjyg2uBCkzjYy2rN9iUtbSQVo194H3ZHKNhpcOnFHW8zYO4tdM2MQK-ZPlE2y7FvzUxp5YNkdKKce-VnFXMLPYDawR-uKFNePC_1XRE5Uamuvxbtk-q_aNz7UqhkSdBn8WQ92CL38qmIfBlkSYEEv95moul5QFweWbvgHdErvKdQOXR5vNzliFB-ye6hJmeSVHp2_V2IVNCb0mz0Se-ZQPHGaGsKgyqKpPTIppnUSrhCSihxSs7uHBDdopeYzBMkHDsjc-AnJRx7-38aNEZniZx_O22CtnGrawDuSRd4vk_DpJ-C-r6SD5uF6frhj9uauyaKfK9gsj9kkZwOEucxKkUFa_JpDYqGONCaB86YVt3j8Vd10PFTDfSJM5sBqdE4q3hwKVDjoC-dorYpYkAgHCJvPiZZy4RRqf-13IDbrUolkqtcZ6t1lnKs_dt6eN42Hz0pWWnrPgDwYT3tVNWZt8iieTSl94D1m3nteNE5Tf7kr2gkBsSQgPvFV1H68HsSCsEL6twXyZLIhKrbuqToTeByr9aZUk8-ThdYDC7SWRqQlf3kBPE-T8OPasNb3wkyd2KAQ-SKSYov49hqGZR49qLgEyN3htxkugNlz6NEU4rUiexBpIXXUthTK-BojHL0IJxQXvFRi68NartMut72btgvcsmhCXjdlvmH1l-RysYfHk-b0dRXh1qxu0B5apDhDYM8b1e09NW_D8hFCsX7NKI8MOmsmjO_M3sdibLc9yaaQ005TywSYZNtabvOHw2CTEilih9_qS5KyimVXsMkHch7PrZDh9Z_iDf-S1IDISxzucV485ONSgb3-wD55hO7hwaUI7-uDXfvDR4vTgKDC_sL4EX4ROUiZRm6KXi_kqpUsInS17rtws_pH2EVdqZTDPRYOcxe5NLudITYClEdtYPfynDaSdnL9mV643vFSJIF_1dmQFDy1-a3RmtEJNz9EcEstN0icCdMWrY2B2ySgXBbt8Gq-posCeo2_xp4zqNh2RbMCLSmucNV4q_8YOYQdQ7y9fwV_5gLZyvm4zAtay62ym5ynxWpyR11-H-yfsa8k9yt4_qMaxlWiMIfGZfak3MPzRsXYMxN-nsD1b7MPqnrMwe4tgliRn6XBMw_6X8UUlGfh_4A8d_lQjPGEvvm4ynvz-AaKU8sOYOIJsNoZ1wse2nBeLzZxxKAppr0pw8mZzW81v4h2LJbIQzOxAXYMqNPdDbdBv5GrcOUkr43CuDVQxhdBtpjMj4U_avQScX0YJcwm6lyzyRJPNnx09wbHVTDcOmDd_XFcbRXvR5kTWI5V2sX6flwNYlAdNRkGPBZqdfzny6tA5c0FUCtj07aAqOgGYiMiFHlglJ45KcCkoZ3P0BTmSQp3D-xUmhEw1dBN9X14-nhxTz-TP4KcU1WwiXyGue5oseXW--Rg8DhI24SGCMQDvoG5yzFmd0ITCyoi1R1JZ8fr-1eemge_0DKRFZcYA8FWav_gD1AGgR8wEPekyyIXnmnrHDozD8rT0Sn5IyfLvCPZCfpTupT814YdnIIxHj5tUCmORDk6AS7cLQPcJMqL182uOfbjcY7Q4TIafyJrKCtSztNSsxaltHprkp0btm80JjnKWIogRAYFfMa8BRtQmIL6Y4u3X8CqkYvve44e4TRlX03iJCWT4S1vh4_pN-rTr7OuUNpqGSs91n617Zze3zhgvUo-SUGvc-yCTJ3oiI0Sii15iA10oDy0H3xLcO7u6j34PfI8amgeGp73Cz9URvle17yV2MOJKVpxscNKkCg3pXs2CYFwVvtq4b_4LYd621PaBHIIahZyhDZwQxrikmEHJ-_Lf2hUI6Q-FFXgOj4n7kbZfsitFqIDyzVEm7ORqRQ7yuBlC73Y1kuMekSuLQ6Uf5HmHuVMJIqFD569hjaA7CLG-Lh-s89rCxuuDY7QzG1G221snac8HYgOQX3W_WJJwaYlcU4iFbR71Mk2HgMByKqxqUmfDZriWa38rpbA0MjXf_x4ZNh9_f0xD5DZEya0z_mo7ZRBT5ad6BTYzT03e1WzcX4SZCRG4QFfJidgDdRCzkcCNpLo67wqGCHRib_RL6XgAfY1DUS5JxT1mlRxV2zOJOTCVeMYFR1uCybRf0T2TBMH1XmDeZcxWr9gwQX70gPX3PJ916BmTNZP1XQYfU9U6jlJ9XEBaJwcbv37-qxk1jv8OqB44cbL0UHNkgpdt41j2MWEeBadHusXrrL5o7uPTNlLLfiCafgL4FaWHD0phPhYj-eZol4kZ-G9PKyU-yVJXUQ1WW8kGMXL4CHn.PllrQI37esZIYbCBNivE2w");
     

    }

    public async Task UpdateSession()
    {
        var handler = new HttpClientHandler();

        // If you are using .NET Core 3.0+ you can replace `~DecompressionMethods.None` to `DecompressionMethods.All`
        handler.AutomaticDecompression = ~DecompressionMethods.None;
        handler.CookieContainer = cookies;
        // In production code, don't destroy the HttpClient through using, but better use IHttpClientFactory factory or at least reuse an existing HttpClient instance
        // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests
        // https://www.aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
        using (var httpClient = new HttpClient(handler))
        {
            using (var request = new HttpRequestMessage(new HttpMethod("GET"), "https://chat.openai.com/api/auth/session"))
            {
               // request.Headers.TryAddWithoutValidation("cookie", $"__Secure-next-auth.session-token={sessionToken}");
             
               // request.Headers.TryAddWithoutValidation("accept", "*/*");
               // request.Headers.TryAddWithoutValidation("accept-language", "en-US,en;q=0.9,es;q=0.8");
                // request.Headers.TryAddWithoutValidation("cookie", "intercom-device-id-dgkjq2bp=9ab266e7-104f-46df-a8ec-2726d512e855; __Host-next-auth.csrf-token=16594dd4956288954395ba48c00687e3641cc9e09a5e1483efcadcb0d254168e%7C3647ca9d35790a9d43b74f29a045c2fbbe749691582d7319289c55bc2dc02efb; ajs_user_id=nfC9zQAtFYO2fZHxuR2LN67NFtT2; ajs_anonymous_id=2a42d869-61bc-4339-9fdc-b4c6dde89a40; intercom-session-dgkjq2bp=UkZ2RjJkR051alppaWpJdHdETGdxZk5iQjFaRzFVd3NMRWVLV3AvT29zWHAzc2t3RHJmLzJRZGtGejlYODUwTy0tenhRYWtDRmlTOXpEWkNkOElNZmEyUT09--49ed3fda59cb3aa541ad13002d71283c78b8cd52; __Secure-next-auth.callback-url=https%3A%2F%2Fchat.openai.com%2Fchat; mp_d7d7628de9d5e6160010b84db960a7ee_mixpanel=%7B%22distinct_id%22%3A%20%22user-Dl4ifhTmjdWafGAXNhyWPGpr%22%2C%22%24device_id%22%3A%20%2217d39fbf4095ed-0ad3e62e84e519-978183a-144000-17d39fbf40a65e%22%2C%22%24search_engine%22%3A%20%22google%22%2C%22%24initial_referrer%22%3A%20%22https%3A%2F%2Fwww.google.com%2F%22%2C%22%24initial_referring_domain%22%3A%20%22www.google.com%22%2C%22%24user_id%22%3A%20%22user-Dl4ifhTmjdWafGAXNhyWPGpr%22%7D; __Secure-next-auth.session-token=eyJhbGciOiJkaXIiLCJlbmMiOiJBMjU2R0NNIn0..23n9q3kdkCZPUm84.PYZV4SDh6NVuDWh-E5OUL9lqE1b2XvnjGvaY1U-8jA_bJ-McVE1j7PsKa4tvXIR78zNs25BYozoCzc22KRyF1H4hZZixeM5ycoKHSpfPbozfu_xlMshtwIwIqW9gFl0F_kA-I0qbJN18CEL-uLbwSpTAhrrk3GIpr-7QkuRrzGWvhVJYsvSuca5m4nUqFQSmP7it7zkPKUfXYxDqN99JIwlbBgjUl4vOFSZF_6jSH4N30-ifn2k0gBAH62q4VSwElImc84nHFsDtth4IWhn1GQTlsHcQyu_lLwkFO0va8vibyxMNQ9mswzt8I3KBV1JEg0el_IJZSye5FPeTkViIPP4ojjcXsRwtdLrZN4HwCFmxvNKpneDpKbt5qKfLjuHPB1SFsPItdjYA8BwlbKTZa4fAKw6osDdWgPThjJWr1qPtlW1nYlOIxlTsrseGZfLGj4KJt-Wk3OyMOq2zAhidfmP-QE-DXV9JGMazv_zWTt8LRnsOwj4wGTwqeLLauy6sFQYwexaj-RinnefkjbdsCZmPfHKFYKXYRj6bLpvRh9lPWGTRAkRHTw0N6VL6wJbh5y2LvKLH3fTNbYNvXBeGWp0L2xCjSAVlup763_C_XzlNoLr6BF54F8kCCuMTSfsrpjHojKTBmTy44tcRUwL8BVxv9HCH-ZT87VS1y4Dk1D6o1mj_Ns43BPtoPBu-JKNDOT3zChCGQ6nfb5j2N6cwXlFjGMW3dM_hVzqtic9c4IMBkpjpOkF-FZszr9PX8d7eUIrBvfqgmA9WBlFl_TgT0w3O4iGx_oYD4yGfLOSp3TKWkDeGY3PbQo3zuz4LafLvn7DM2oIJ6VnjaMA5OcFbZLY-1__YnRPmJ2xNIcDwxV1B9QnKO76Y272xJVBbShoUdFD7EJ1dp6qt_xJ3Zm-kE3WFLH2JBLtbRFu7SQfmCe24JGIItuEqvHgSvg717EwCPy8sGkAHq-MwEu7OLVx1znFZ4dQ2zBH4xCCRWnXvRtUrCrenY6dAVvQS7LNV1lBc46_uO9P5bY7AD7GnfveiZ0nLUbhnJCZwXOE4bNKFMd08p77bHJyuYO4lDuXwEt--_18ZP0lQTFcniewFUWluPvZS5bUD-trbwTrxLl9gYiRcggMDFrDiKcmKzexLr1x6SY_sPt0OBNfaV4IINt89gW9yqavuhBjNgGFVt-LhIrKuhrCuZQQIL36C4M1uhKjVIT3aKR_T6J-_x268jgz9IWw90Vyc-RpiORZuzv_QWt0pMI8bUdQCY-8FSFdFGk895o8-BlcENjmXP18KYbhcsF2bl7fYDgXnB9ojnLiPa3iGYy4EHRIeEESFBRMqcHj2KXxnEgbT-mIf2X_84epLWy0VAZ_3LeGLvb_gpGExSMHEG1Z9hSav5PX4dRPskxGi7nlOF2uZPtaBQ2ToII4u5w0cczIsVhslR6Eopg2Bq93nKFzRTJtQBwPle-JxBnNk5d8euvPuw6MzFOiOIImJiPAb01I4fWjtp97NyyXZD4aNCt6jHN-d7j9uWcV0mJ_afPbuSIH6yI2kcDNUJ0WFwvsXSFp3lM0E47WS8YTXwiKkKM2TFVt5IQzlVBz9kgR91K__i8x-jxHJDYqmywUJ80csgG-8xb8PM57ycSKMqx4nUt7lvsyRD78v1Qb0y4y8gAhGzPeozJklzBkSsf8m7-obYbVldztww-d04QTlm7YJUfVcnwzbDbSqpbqhEv64mDq9rKJmaF6TiAxfTjDgnFgZNpe47lGSsbLGZ0G0PlWsywfBA7uwoIHcJq6U6jL4t4NF1Na8xnr78uiFs-FmOMNdyiaQhGCFCFtutVd70bovVfHB68F9uBxl1qxaQ9bv6YFsnblzaz_S8uxGQa9ZtkbNT7eg0plO9DtzPGTDqMYPI0NvwfGJPGo2rFfVacVEJONhnHUJ-c9UdkYKPLaJfl4X8PH7RVnUr_ht7NF1As0Ttk85aAfUXAnlqcOXU7har4t5xvhSRI8aIU2JP-HPJizu788xfxfUxHQKshCx2zvQtnF2BKAtRrsGCZK0pPwGwmL3G3gRuJPEYtz97luwe9iS2-lw-KNgJopYnR053q3zxCoH9yKQ3p0_mXK4XvsuYWsCHC9GtL9A_ZRKffOysOEjFVBTU1neIzB4U0eZdFOqwR777LO5xNWBxoV1yhUBLF27Y6zxZALoPa7gwrVXRAGmHrXRG5S-A8VgUNiIqr_lgMjNLcm3jEKNMHGI_7I6LMAjLpylDLMCd8_m9Skz3jqwgBZLIWDR0GYjdVRDe7BVy5co_V7hTO_ojsE-FNJ4CiDwV8MDektixjf0NWd5Ictf.b4TRsp39LcW_IOK8zIuq8w");
               // request.Headers.TryAddWithoutValidation("X -Openai-Assistant-App-Id", "");
              //  request.Headers.TryAddWithoutValidation("referer", "https://chat.openai.com/chat");
               
                request.Headers.TryAddWithoutValidation("user-agent", "Mozilla/5.1 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Mobile Safari/537.36");
                try
                {
                    var response = await httpClient.SendAsync(request);
                    var cooks = response.Headers.GetValues("set-cookie").ToList();//.Where(v=>v=="__Secure-next-auth.session-token").FirstOrDefault();
                    cookie = cooks[0];
                    sessionToken = cooks[0].Substring(cooks[0].IndexOf("="));
                    sessionToken = sessionToken.Substring(0, sessionToken.IndexOf(";"));
                    var str = await response.Content.ReadAsStringAsync();
                    session = JsonConvert.DeserializeObject<Session>(str);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error refreshing session token", ex);
                }
            }
        }
    }


    public async Task<string> Next(string msg)
    {
        // return "Failure";
        if (session == null)
        {
            await UpdateSession();
            await Task.Delay(1000);
        }
        var result = await Next2(msg);
        var idx = result.IndexOf("data: [DONE]");
        if (idx == -1)
        {
            Console.WriteLine(result); ;
            return "Failure " + result;
        }
        result = result.Substring(0, idx);
        result = result.Substring(result.LastIndexOf("data:") + 6).Trim();
        //      result = result.Remove("{\"message\": ");
        //result = result[0..^1];
        try
        {
            lastResponse = JsonConvert.DeserializeObject<ResponseMessage>(result);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return "Failure " + e.Message.ToString();
        }
        return lastResponse.message.content.parts.Join(" ");
    }
        
    

    
    /// <summary>
    /// Sends a chat and gets one back
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task<string> Next2(string msg)
    {
   
        HttpClientHandler handler = new HttpClientHandler();
        handler.AutomaticDecompression = DecompressionMethods.All;
    handler.CookieContainer = cookies;
        HttpClient client = new HttpClient(handler);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://chat.openai.com/backend-api/conversation");

        request.Headers.Add("authority", "chat.openai.com");
        request.Headers.Add("accept", "text/event-stream");
        request.Headers.Add("accept-language", "en-US,en;q=0.9,es;q=0.8");
        request.Headers.Add("authorization", "Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6Ik1UaEVOVUpHTkVNMVFURTRNMEZCTWpkQ05UZzVNRFUxUlRVd1FVSkRNRU13UmtGRVFrRXpSZyJ9.eyJodHRwczovL2FwaS5vcGVuYWkuY29tL3Byb2ZpbGUiOnsiZW1haWwiOiJib2JAZGFiYnIuY29tIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsImdlb2lwX2NvdW50cnkiOiJVUyJ9LCJodHRwczovL2FwaS5vcGVuYWkuY29tL2F1dGgiOnsidXNlcl9pZCI6InVzZXItRGw0aWZoVG1qZFdhZkdBWE5oeVdQR3ByIn0sImlzcyI6Imh0dHBzOi8vYXV0aDAub3BlbmFpLmNvbS8iLCJzdWIiOiJhdXRoMHw2MmY4NGM2NzRlNmExOGZkNmEwNmE1YzkiLCJhdWQiOlsiaHR0cHM6Ly9hcGkub3BlbmFpLmNvbS92MSIsImh0dHBzOi8vb3BlbmFpLmF1dGgwLmNvbS91c2VyaW5mbyJdLCJpYXQiOjE2NzA1MTUwMTQsImV4cCI6MTY3MDYwMTQxNCwiYXpwIjoiVGRKSWNiZTE2V29USHROOTVueXl3aDVFNHlPbzZJdEciLCJzY29wZSI6Im9wZW5pZCBlbWFpbCBwcm9maWxlIG1vZGVsLnJlYWQgbW9kZWwucmVxdWVzdCBvcmdhbml6YXRpb24ucmVhZCBvZmZsaW5lX2FjY2VzcyJ9.wVoWvwuJRrezrp3-IT0y9bZEhHjav6A06hMzN7PrfulzAdDbtkqFxuMdXlMJZx1OTRfL3oyloIP4ZtDmCA32oV11iKlsB-uBcYITRoCwcpkGELim0zgZ3nYna2nL11x9qE8UqaPfHyOSIDtWA8IvNXoRDe7XqBzc9CyzaIVR5wE0TBMUkmk1iytprgnJjpud9qtFQMHURpUhRWPwRZ2byMIdNK6TNU7VH-N6AQcnzV_CxBHLe8sd2ma9GIMPY1eGmAJaN7GS2F5K_47nqBCW2qpOvq8FyODwudQiXR9cBxx7-Ie6WRrZKG_ZJ_30wQbP8hMLaiynH7Tc2XG29vV1BQ");
        request.Headers.Add("cookie", "intercom-device-id-dgkjq2bp=9ab266e7-104f-46df-a8ec-2726d512e855; __Host-next-auth.csrf-token=16594dd4956288954395ba48c00687e3641cc9e09a5e1483efcadcb0d254168e%7C3647ca9d35790a9d43b74f29a045c2fbbe749691582d7319289c55bc2dc02efb; ajs_user_id=nfC9zQAtFYO2fZHxuR2LN67NFtT2; ajs_anonymous_id=2a42d869-61bc-4339-9fdc-b4c6dde89a40; intercom-session-dgkjq2bp=UkZ2RjJkR051alppaWpJdHdETGdxZk5iQjFaRzFVd3NMRWVLV3AvT29zWHAzc2t3RHJmLzJRZGtGejlYODUwTy0tenhRYWtDRmlTOXpEWkNkOElNZmEyUT09--49ed3fda59cb3aa541ad13002d71283c78b8cd52; mp_d7d7628de9d5e6160010b84db960a7ee_mixpanel=%7B%22distinct_id%22%3A%20%22user-Dl4ifhTmjdWafGAXNhyWPGpr%22%2C%22%24device_id%22%3A%20%2217d39fbf4095ed-0ad3e62e84e519-978183a-144000-17d39fbf40a65e%22%2C%22%24search_engine%22%3A%20%22google%22%2C%22%24initial_referrer%22%3A%20%22https%3A%2F%2Fwww.google.com%2F%22%2C%22%24initial_referring_domain%22%3A%20%22www.google.com%22%2C%22%24user_id%22%3A%20%22user-Dl4ifhTmjdWafGAXNhyWPGpr%22%7D; __Secure-next-auth.callback-url=https%3A%2F%2Fchat.openai.com%2Fchat; __Secure-next-auth.session-token=eyJhbGciOiJkaXIiLCJlbmMiOiJBMjU2R0NNIn0..hR0EYiOF_oqGlhLF.izM0KNzbXxBraKwxo7EZfd4ok5Pnai3I-xRDeneiysYR_2cWRrsqrGzpNSk2-nsXpJUIBLqR3RhZna_Wn1qVE_3VtQ6SJVIc9H8N71Fd6wWWOgFZuzrUICjgmouMmy6hLqyZDoy4yqiAABR6UBE8cz-kkMlnCanhOH7HD3wj4jH3sBD-OPv-On0OwkosGUzwFgmbhokE5MwMF2st4fDm0Vs0cufeysgIjRcdBApBwh6PhNQkBdYKs8gJfU2qNoiaZfdMH5Zbyu5GFbwVL8M1EAFfCISLmYeBRI5zJ2KD9nbKHfUPrwX_eEQXT1UCLQ90TfMCyNlCnIxRxVgzr1NRaD0iV3cxvWM-XZasPuquKgZniYfpxbnw6l87HaczwPDQ5Ms76Dsu6M06qP2biwWYI-ZQBByKQTcIsl_B55l8AOT3wmLRNmY5y4nP_ruKCIIyrlxXb3io2XvL7vtFyR0DrhSITiNLkMHIS3tl4VRC-zQ4CP-FMnHzgvxoIsvMeAS0CKzNNglavrsDu9fHfk015Zj3C1ATuz_BEFODJBVaU56-BXAPtR2CVdgvpHzcO6jafv6rNJ7KLAz-Pre2Y5O0Pl0meT4DaoUewq_-s_mGKucYdASZPGGAQI5_-W7t9dwgo9sZXLcy8S7gwFwF-dNCVoudGp3bcoZGUoGPACAGzMpwRGudHprvkdhZjs7jJTPmPjZ4m6ywLL_iOBfQ6I9QkNmvSjjdNPI_8QY4IcCV9Sbp5tCprVntWoBxZ_aV0TfYsOtjm1aogyvfJmr6giZ4CKQCf0dCV9-0TyG0XmKjF9VE8dyHP_q-RqkzpDeW7pkbrqCL_3CnEIxXVcMU4LJ0oS9XfAiuya9HThmEw6RNI79BoyhZt-Vp0B68GImcLBtMc-cItvgJ9JPUFEqjEZzSD4PDsxtZT8dEeCLvnWrpAq8ajoFNh-fN4cso_v3C_8Z0w05w9hbJcvUEVE0LOpFS9gMckeuPJOkmDLx3V20QjV_RkYRAshdxkrwAIs8wiZSQBfK_4R13gAi7YMvXoZ4rb9fD-u-5gpMEXvDCdeyapPz3Xe0RmX8diHCJX1g_oreAEAAPFp7BywFTlhuQ8dvRQ7FY74udaVv0_JezGlbbwiVxXsBiLkHb36yne-7_0foSdn17waEE42DPPS64Kld0LY50C_6bXHjDFfCyL-qbCYMOxRc3dUilJNbhANjZysYY2hT9Ru70VPTdyQM0eMWEX8ZnQheSxa3BWaNC1cvB1IrIO08F4JSW4R8ap0Gw5WIIGYwlcg8AmHACBDbHeo6SiHi4VykvCEtG0quajSVxtlT7C7XhxAvtz5IDgPiE4qnnklgRrXIr-8aev-DK7ZVlv8ivXhdrQdxsiA7IpwaFM74rGMOg9hg7r56w23b3WkUx9o237yz3lo0jJP3LQ0R1pzZBBB_o7v-MMQPTeKDNbsJLz4grFyuc2Du2sNL5LymLPzARlELyDfAQNo6cUiBka2c9BWPH3Dvup6Bv4APT3lFO6mazJDVnha_BNb3iZAgtODE6-qnU0pM7BPJ9bKv82E7hGmdFYg1ysmwauFXFAQK69SLAtkmPE8qmXgdggzYrNbJFiAqoaeOlkJT4BxYjgIHIkXCkVARX33a1tqCBjxS8qrUoqD5nSoeJN3Yj_2uMJvtZf9VSVx7ZQEp_MntHFi1zok7cJoRgP_kZ3gSbxnwW64v_7S_TBrLP5h8PbaQN9ISUsk_JWoxm0wZ3-O1PmwOLxYY9gNeTCbJLe8_T8F9fPulmmT6v5fOYIAM3pvhSVblwB0-UV2Xw1_6E4xQoP9a3O8QUaV2j92oASYwhYF-DhO0d3u2WrhGB34dv3C_11GI1vvxWwudXguey_-Ha7Vvn4rEY39-Ttffzezv1bDaQ51hlwVz6coD49_g5_C2wDGGgEu0mMyYHWR3uNfCC_UuL7zvRgyHH-1ZBzldtqE34KdNO1ipS3tTv2-hcpDFk08ikkYjHYGRZb7oIHtnxZ6Xb_lkxndthJFjo8wozfeFFaxlXRNDF5RU_Ge1S40LoD7qNS_L6R80C0X5Hv7mcaBlVRW3cwhvkI2n4MyRkJgh0VstKNBHincjoajaOfvTiPC-WU_7gXGH454NlNYkzi7fQSOP3eB9LbYIWd03GrVhn2bkrtoV42QSJ8mCe9sLUrEQgeRn6237Zo9c-prfvWTuASxpdzdpsfnYQvLton8Sfy8NbACsdOTf6jgpbXdRBhOSkiH6arGNeYLhM1ilFOklOEW9hXzmC_TcCpS7EF361-t22hdNmGJkEjka7bDYVCLCRJNDWiVMJqkyxAk4703Tb.wXJKMgFu8HtSP60IwZjGCQ");
        request.Headers.Add("origin", "https://chat.openai.com");
        request.Headers.Add("referer", "https://chat.openai.com/chat");
        request.Headers.Add("sec-ch-ua", "\"Not?A_Brand\";v=\"8\", \"Chromium\";v=\"108\", \"Google Chrome\";v=\"108\"");
        request.Headers.Add("sec-ch-ua-mobile", "?1");
        request.Headers.Add("sec-ch-ua-platform", "\"Android\"");
        request.Headers.Add("sec-fetch-dest", "empty");
        request.Headers.Add("sec-fetch-mode", "cors");
        request.Headers.Add("sec-fetch-site", "same-origin");
        request.Headers.Add("user-agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Mobile Safari/537.36");
        request.Headers.Add("x-openai-assistant-app-id", "");
      //  var id = Guid.NewGuid.ToString();// "4faa4009-845f-4a57-a466-a8aeffbdd883";
     //   var parent = "5da94812-6ccd-4d83-bf13-944febce86f7";

        var id = Guid.NewGuid().ToString();
        var parent = lastResponse?.parent_message_id ?? lastResponse?.message?.id ?? Guid.NewGuid().ToString();// "f0327544-c741-43b2-8461-605662888066";
                                                                                                               //lastResponse.id;
        msg = $"'{msg}' he asked.";

        var str="{\"action\":\"next\",\"messages\":[{\"id\":\"" + id + "\",\"role\":\"user\",\"content\":{\"content_type\":\"text\",\"parts\":[\"" + msg.Remove("\n") + "\"]}}]," + (lastResponse != null ? $"\"conversation_id\":\"{lastResponse?.conversation_id ?? ""}\"," : "") + "\"parent_message_id\":\"" + parent + "\",\"model\":\"text-davinci-002-render\"}";

        request.Content = new StringContent(str);
        // request.Content = new StringContent("{\"action\":\"next\",\"messages\":[{\"id\":\""+id+"\",\"role\":\"user\",\"content\":{\"content_type\":\"text\",\"parts\":[\""+msg+"\"]}}],\"parent_message_id\":\""+parent+"\",\"model\":\"text-davinci-002-render\"}");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        HttpResponseMessage response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        return responseBody;
    }


}




public class ResponseMessage
{
    public string id { get; set; }
    public string role { get; set; }
    public object user { get; set; }
    public object create_time { get; set; }
    public object update_time { get; set; }
    public string conversation_id { get; set; }
    public string parent_message_id { get; set; }
    public Message message { get; set; }
    public Content content { get; set; }
    public object end_turn { get; set; }
    public double weight { get; set; }
    public Metadata metadata { get; set; }
    public string recipient { get; set; } = "all";
}

public class Metadata
{
}

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class Content
{
    public string content_type { get; set; } = "text";
    public List<string> parts { get; set; } = new List<string>(); // the message
}

public class Message
{
    public string id { get; set; } = "3bed02ae-07af-4d33-8eca-316f3c2651b5";
    public string role { get; set; } = "user"; // or assistant or system or tool or unknown
    public Content content { get; set; } = new Content();
}

public class ChatMessage
{
    public string action { get; set; } = "next";
    public List<Message> messages { get; set; } = new List<Message>();
    public string conversation_id { get; set; } = "4c699ef3-8403-4c61-aaee-0f5cce56a0d5";
    public string parent_message_id { get; set; } = "6e22a68c-1f31-4e7a-9bad-be22f05b9542";
    public string model { get; set; } = "text-davinci-002-render";
    public object error { get; set; }
    public ChatMessage(string msg) { messages.Add(new Message() { content = new Content() { parts = new List<string>() { msg } } }); }
}



// session stuff
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class Session
{
    public User user { get; set; }
    public DateTime expires { get; set; }
    public string accessToken { get; set; }
}

public class User
{
    public string id { get; set; }
    public string name { get; set; }
    public string email { get; set; }
    public string image { get; set; }
    public string picture { get; set; }
    public List<string> groups { get; set; }
    public List<object> features { get; set; }
}

