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
//using Microsoft.AspNetCore.Mvc.Filters;

using DibbrBot;
//using Twilio.Jwt.AccessToken;
using System.Net;
using static ChatGPT3.ChatGPT;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Linq;


namespace ChatGPT3;

public class ChatGPT
{
    public CookieContainer cookies = new CookieContainer();
   // public string cookie = "intercom-device-id-dgkjq2bp=9ab266e7-104f-46df-a8ec-2726d512e855; __Host-next-auth.csrf-token=16594dd4956288954395ba48c00687e3641cc9e09a5e1483efcadcb0d254168e%7C3647ca9d35790a9d43b74f29a045c2fbbe749691582d7319289c55bc2dc02efb; ajs_user_id=nfC9zQAtFYO2fZHxuR2LN67NFtT2; ajs_anonymous_id=2a42d869-61bc-4339-9fdc-b4c6dde89a40; intercom-session-dgkjq2bp=UkZ2RjJkR051alppaWpJdHdETGdxZk5iQjFaRzFVd3NMRWVLV3AvT29zWHAzc2t3RHJmLzJRZGtGejlYODUwTy0tenhRYWtDRmlTOXpEWkNkOElNZmEyUT09--49ed3fda59cb3aa541ad13002d71283c78b8cd52; __Secure-next-auth.callback-url=https%3A%2F%2Fchat.openai.com%2Fchat; mp_d7d7628de9d5e6160010b84db960a7ee_mixpanel=%7B%22distinct_id%22%3A%20%22user-Dl4ifhTmjdWafGAXNhyWPGpr%22%2C%22%24device_id%22%3A%20%2217d39fbf4095ed-0ad3e62e84e519-978183a-144000-17d39fbf40a65e%22%2C%22%24search_engine%22%3A%20%22google%22%2C%22%24initial_referrer%22%3A%20%22https%3A%2F%2Fwww.google.com%2F%22%2C%22%24initial_referring_domain%22%3A%20%22www.google.com%22%2C%22%24user_id%22%3A%20%22user-Dl4ifhTmjdWafGAXNhyWPGpr%22%7D; __Secure-next-auth.session-token=eyJhbGciOiJkaXIiLCJlbmMiOiJBMjU2R0NNIn0..taTydEf9_b4-09nb.Pm929co2VQzWZ-jmEjiSbyf2Rn2Y50QILr64Fliubhva1rqvSP_gbvumalq-nNkE6yEzqDxoJkSevMcQ-Gqh4Azha8bNMeHjbiJcV5o0D0TLz1ZDF4C7VxxfFKJkX8XrxkBZxF4GtKYWeWrirxyk18R1l9hdhICVU0rNeBKlVLGCH3ZlOUb7o4JVqE9UDiu-jJMwr29CSWv0lH_go9X6l7kiADTC-mHA847jz4bNYrbrGzqgKpkaZmnRb0C0WTxn4yh64s0ne0svEv2ZQDSYF7JLRGUhFMK1ekWcpV-HkZPer9vdSM_aJ3KeMfkNWpubr1b-n_xkobZH1CdMPW2yG-_kXTaJP_nLVse_WE4AH-tgs6A100o4Il1ZMir3gtdwjY6xCOOXBlde_P97bvNCnFbW7CAW0XnoPhXL34kRijchS3LAOcoWYzWBkE_LN8moG4Mvw70SccB8ZPoNdxUG3ZfcULQy9IFLJ5dZsThXf2SP6hMlYWQdKZr2FvG9spWIaQG4ELehTZ6M1dsr2VsNKmAIDsj22CgH_N5lvN-janA5YL9JrR9oEVX5lbZHU76sB3DhSBYHey2iGydPZg_-Un6ch6GnWgH0h3XiTiqz1Gkl0VEWv90_SuqsEI0INxCQoTjnnFh0eG3VdaWpmtL5feH3G9m7JI8-7xCHzB-CFrTUxazH_aLTidGJTb5H4gwasQmyooUJp58DihLaGQZBFq7LAWD-VnwtWmxh2o4WIgtmgq2YSrfJ4Uo_q_eWMQqvCykbHiz3VGLQ8FJDPLGQWhNy95k0TNVUPe6jTYT5TdHkPbt64ZiwF472TOLWbABQWKwxbUIGTnnIAvQh7ljZ2ahf-efwG12sm-r72ANYbZe0E-WvIs921960vtE_OYxZdPl_yyxTFUFah9VdseNiiLQhHOJMhZ9nJjn6IxVXNMcYLNgJ55iUpKur8x15GX-gnbXFtsd-bPONZwpK3mMwO1ZTOGRyG1nn-wK6hof3FOvQfMWp-MEyWki366VAI221f3J-VGZ8LV3B-jaYm0OZnggRe32DLyaHZSxB2hQUBhb_FJkFXScbClm56XuPdfx_csqoBTKvy3-ac3NUrIAAEUM7tw3lXYoQ92ufiQDewottbjr3ZVtty_LiwkyiQk-rsfgwgDJg2Bp37PRmi6caYKXZ6XG869v3x3oZRh_oi7PDW4gzq79CYQYFmZRUMKCK8QdAHrAhLj-FcI7sejRChVth4hcT6HAGiItmi108KK1tI1zLKt2f3TltKDLocQMizNFG1NT4Lr17wFXZdwm7czCOyI_g3wfh-22CnqFSBTMrH6H4KYyW3OJhOeyr0-x_2XLM7bKA3ygVR-Ys6mf0fVSwzU4Ata6l0JuKqIS5LDgZ3z9-qZ6guw3Xq7U-Qdxm0wQHvRYG6hH7GPnGYOlItRA-Y2Vm3XctjABVBlXLC5DFFfSwZsTLYA_HOmNveXJEM-8AIBnht5fmPwky_rekTu-km1eH3aRCHTwYIcb5EnVlfcwxU9ZLeBufZewHtuDAppqkRr4TYn25VqnCiwVq5Mkj_KSHj_CDLTOq4kEci6utHexHYq45JtmMNqL7UfbGa6591xuL6NV8fNSriIvIxk5ThEkyDUTtefIgq_byAphLh_X3AOpIOs1zULw3qo9Zr9kQsJB9lfxYROKgXcybRFihIQIfBHm1FJZOp4E5-z9WVbh6WAy6SKNqaRJfl8dtPwpncrE6fBFidd3apColkvmg-Qa532U6uCIgNR7OLoOrsThCGCxy8SEaEvtrjpHRRx1oQUc1ZnWMOxr23cdNuif3xwhUYdLuD-r1KWE1P5pP1zZMNQayyL6AR9xtubGptjz1SvLRyxGE0IKqkyMY1sEr6xGJeeX1q5RKtjEK6rizeLI0jNPT-HUti010fpEgFayBgZzJ-sM4iBnyrLCIqaV5QW7xOQqH4btAiTm7i9BRHm2aRBxEB9WzQ8jfUt9MGC7PtZ3mjbgb0T8KoElVwjQzQU2bdDUc4RY9zVKyUpYtepuzfhvvk3qSDbimCu-5R3k9KOgxR4JnnivfFY0AvDI6pGWIFzOTaqZKb56kGCXs3PkpZpc4JHOjj2HHV3ENQ8WoSG1BBgyabZKwG4nXUisdkXV8LgzCdKqGRyQScfnVuun356bEGkzBex791ckmZxr1VEVBGTMntCq2zCu7J1gYK5PU7hNDPTdHeMa565p9CNS1CxkHzqmuJqSi5UmXewcGGWjtc_4cmM1lPUrve0859QDArpWiLBtmUzYC_vta4l8wfe_4k7UZup5XgX_dfU41eXvqk755IN8uSPacc_1h.6jHOjQgdSd0Cmeb9ztkegA";   
    public Session session { get; set; }
   // public string sessionToken = "eyJhbGciOiJkaXIiLCJlbmMiOiJBMjU2R0NNIn0..taTydEf9_b4-09nb.Pm929co2VQzWZ-jmEjiSbyf2Rn2Y50QILr64Fliubhva1rqvSP_gbvumalq-nNkE6yEzqDxoJkSevMcQ-Gqh4Azha8bNMeHjbiJcV5o0D0TLz1ZDF4C7VxxfFKJkX8XrxkBZxF4GtKYWeWrirxyk18R1l9hdhICVU0rNeBKlVLGCH3ZlOUb7o4JVqE9UDiu-jJMwr29CSWv0lH_go9X6l7kiADTC-mHA847jz4bNYrbrGzqgKpkaZmnRb0C0WTxn4yh64s0ne0svEv2ZQDSYF7JLRGUhFMK1ekWcpV-HkZPer9vdSM_aJ3KeMfkNWpubr1b-n_xkobZH1CdMPW2yG-_kXTaJP_nLVse_WE4AH-tgs6A100o4Il1ZMir3gtdwjY6xCOOXBlde_P97bvNCnFbW7CAW0XnoPhXL34kRijchS3LAOcoWYzWBkE_LN8moG4Mvw70SccB8ZPoNdxUG3ZfcULQy9IFLJ5dZsThXf2SP6hMlYWQdKZr2FvG9spWIaQG4ELehTZ6M1dsr2VsNKmAIDsj22CgH_N5lvN-janA5YL9JrR9oEVX5lbZHU76sB3DhSBYHey2iGydPZg_-Un6ch6GnWgH0h3XiTiqz1Gkl0VEWv90_SuqsEI0INxCQoTjnnFh0eG3VdaWpmtL5feH3G9m7JI8-7xCHzB-CFrTUxazH_aLTidGJTb5H4gwasQmyooUJp58DihLaGQZBFq7LAWD-VnwtWmxh2o4WIgtmgq2YSrfJ4Uo_q_eWMQqvCykbHiz3VGLQ8FJDPLGQWhNy95k0TNVUPe6jTYT5TdHkPbt64ZiwF472TOLWbABQWKwxbUIGTnnIAvQh7ljZ2ahf-efwG12sm-r72ANYbZe0E-WvIs921960vtE_OYxZdPl_yyxTFUFah9VdseNiiLQhHOJMhZ9nJjn6IxVXNMcYLNgJ55iUpKur8x15GX-gnbXFtsd-bPONZwpK3mMwO1ZTOGRyG1nn-wK6hof3FOvQfMWp-MEyWki366VAI221f3J-VGZ8LV3B-jaYm0OZnggRe32DLyaHZSxB2hQUBhb_FJkFXScbClm56XuPdfx_csqoBTKvy3-ac3NUrIAAEUM7tw3lXYoQ92ufiQDewottbjr3ZVtty_LiwkyiQk-rsfgwgDJg2Bp37PRmi6caYKXZ6XG869v3x3oZRh_oi7PDW4gzq79CYQYFmZRUMKCK8QdAHrAhLj-FcI7sejRChVth4hcT6HAGiItmi108KK1tI1zLKt2f3TltKDLocQMizNFG1NT4Lr17wFXZdwm7czCOyI_g3wfh-22CnqFSBTMrH6H4KYyW3OJhOeyr0-x_2XLM7bKA3ygVR-Ys6mf0fVSwzU4Ata6l0JuKqIS5LDgZ3z9-qZ6guw3Xq7U-Qdxm0wQHvRYG6hH7GPnGYOlItRA-Y2Vm3XctjABVBlXLC5DFFfSwZsTLYA_HOmNveXJEM-8AIBnht5fmPwky_rekTu-km1eH3aRCHTwYIcb5EnVlfcwxU9ZLeBufZewHtuDAppqkRr4TYn25VqnCiwVq5Mkj_KSHj_CDLTOq4kEci6utHexHYq45JtmMNqL7UfbGa6591xuL6NV8fNSriIvIxk5ThEkyDUTtefIgq_byAphLh_X3AOpIOs1zULw3qo9Zr9kQsJB9lfxYROKgXcybRFihIQIfBHm1FJZOp4E5-z9WVbh6WAy6SKNqaRJfl8dtPwpncrE6fBFidd3apColkvmg-Qa532U6uCIgNR7OLoOrsThCGCxy8SEaEvtrjpHRRx1oQUc1ZnWMOxr23cdNuif3xwhUYdLuD-r1KWE1P5pP1zZMNQayyL6AR9xtubGptjz1SvLRyxGE0IKqkyMY1sEr6xGJeeX1q5RKtjEK6rizeLI0jNPT-HUti010fpEgFayBgZzJ-sM4iBnyrLCIqaV5QW7xOQqH4btAiTm7i9BRHm2aRBxEB9WzQ8jfUt9MGC7PtZ3mjbgb0T8KoElVwjQzQU2bdDUc4RY9zVKyUpYtepuzfhvvk3qSDbimCu-5R3k9KOgxR4JnnivfFY0AvDI6pGWIFzOTaqZKb56kGCXs3PkpZpc4JHOjj2HHV3ENQ8WoSG1BBgyabZKwG4nXUisdkXV8LgzCdKqGRyQScfnVuun356bEGkzBex791ckmZxr1VEVBGTMntCq2zCu7J1gYK5PU7hNDPTdHeMa565p9CNS1CxkHzqmuJqSi5UmXewcGGWjtc_4cmM1lPUrve0859QDArpWiLBtmUzYC_vta4l8wfe_4k7UZup5XgX_dfU41eXvqk755IN8uSPacc_1h.6jHOjQgdSd0Cmeb9ztkegA";
    ResponseMessage lastResponse;
    public string url = "https://chat.openai.com/backend-api/conversation";

    public ChatGPT()
    {
        UpdateCookies(
            "__Secure-next-auth.session-token=eyJhbGciOiJkaXIiLCJlbmMiOiJBMjU2R0NNIn0..nJqgIgjt4adWFXt9.B7TRD5iho8ro1PTNar_rklyMNdKr1e6xo9ce9MqJ6vQihwAQ2eD1Sr2_3nNelGB-Sfa4KWWrxDQwvexclnRl8u4j8Gqi_akhZ5JOZ1v-BBhLExjUnd0ximLWxvO1k_HFf2lToRl_zajBvo87ct5vuZaA1uH7xV8RrTspz8JHAdt7eB_SGJg1xDr7z3mb1az59PguIQlyRJWHK88avIeAyJZb27F-Q1S3qeLbNbqbg2dr2UmkkGcj4-kLOmdW-741nVxKYlXY9sNDuiKBer-oW-uHf3LiSlieCqcB2T_ABHB16Gh9Qb0MIg3_FAVbKJQppa60AC-WTPd0YZ2nYHG0GLmL-e3aBYltYXDpNQoxtvR0CdyZs5bCKTPii4vCl-zleuZRXsr4RybNZ5DcBlLZA5w8FGg8WZXJ9yqr_lQTMA6cmBE7aI7vCqiPbgGfEmIx6sFZgMdHWg8xbGTNhwSO9qgQyututW1idBqQh_E6VTJ-aKWFyY5MjNGtSuBeGOwQXLItc1OgjYBupEz9F3w6ktTjuEPlH7Hvfqi4Pk2DU4vHmpEHGRzNeQKgLiC1zIfVia6ES7kYMUIDxDyf3aoUuGHrZr9TFQoQILmCX2Y6BWA7WXQvzrgy_xhxPABun4BJ3H1uZ79ISbmdxF8B9Qo_GMIsoaijg3WPyVUO9Ukh1CFK9_YWS9FnOQYuwcexLabkve-vA93LB3MWhdi-0NhDqVijVPgB7VUaTkMlksr_nMCUJdwYZN0dmGuI4TTRWuDI7bzlgex9j62wHBjUQb2Bvcy0MWRZqOHkxObn9SgQYJ--md84tXKwaORb76l6PXYz0kf6EOaveic41liTlbosJg0aJy58zzEx7PQMTZSWB8LE4wHnRKrmkyTuxQvhNOJPMUa-1Lh7nuEQjl2ef5q-uZDUtBVI2HLI0fOGYO7iBV5WphIkeAjbjP8bAbvoLmMAIoCy_0Oh33_HuCGBytcyqrRJAzA4jtxuURbIxVJqEcSr3W-hFPixhoLPm7FEz-E-FqDLxEIiiyG6W69bK3Yf0ltfq0bU_u4p-uSnzKTMhe3xwDD7bvC8a8RNbOeIH2pxF1elnJ4cEbObJCVUl_tH-6xJoidvj9oU9m3G7T86iZW8CyGf64IoRzHuOLQhYQvtBcLZLvyHJC0uOawgtE40gHcM2nTZhNikDfMeygnaswXD-llXBHDqB4VBiY72H9lzwtJz_FJS3E8xZk_SQaiNlwZshIzUjnSnAFr19F-2mE9ckHUhP4freGLn4d0zRIlIgivbdtNEkR08jTYpEryG6fj9FnZ4xY2oIRT0lx2Ci1Mb191lb6m_almUAdIOS74Y6PLeEqZHYxj8NVg-XrIOaEb2_3Z2lSc-vUl0DxT8SQxPTLBKCSH5M46H0PA_v1HEj2a46V6hqPYd4X6Tq2G3BjtnmO1-1H4rXFndfihabsKu61fvFCZ_aLKKYxELgOYJxGvxJkvd5wOVazxVjRpItHphaG6SAsuLTBMfJfzpkMCF9lcTMYEQUE0Jl6ru889M97cLKdhPVmcOhrQF6mAlL_NUHn0EkzwCFzg0hNa0sDcHjSfdhBpIR-T-Jl86sC0SfQ6IZcDJFnUaI9T1UJEVbNdcIedwgKC3Rgj4v9fl9GfGrMEMUpEHgPSeJHrGzNJn56ccPcijbsX9KcRKTGwDcqcSkG2BkNB2kFNh1JFpl2usGNC60VkUqVmb8qnCNPsSDUKeclfPv603cFvqcy42MabkSgsqlJWGSwGcZvkl7CSBmbB47IPVphdfWORaRh8gIg6BqsxCAnnjcMlRhm27R5Pa4YngAenEMJ5CQR3V3iVWLjbfvnEsZuOz_dWmr6IhtMoGQry73aUthFV9Q0UiMQnP18SSnglhrzQFEufiKNtSbv-Di6WwRk_a79GxeG1GteiF7nnqscNX4TT6SjITJtMudixUsxQcNiBI4c_Qd_GFZ4QZTaSim-GA4ehRE0ZDsuf94bRK-hSDaqSamqPUTBTouocMjV77m3JzNJfFJ-hD-SgbAJLrAzXv-bSHuTdCdvbvBz5vZWBvPfNhh0VQGB_jMdazii25BzjaxCGKELFBWd5g563A0vqFeGR8GTzdzVGX5VxbY-kjPkFqd1AKcaA8wvoMp6cDdQMhwOxN9rV3lFGTgkBFBfaRWG6r_5KvYs05WWA0w7uJnLLW7Pgv-HeYAzpnSPZtVbGOrtSKF9Bj46LcnAklP6yjcAUw_Ibcj1niCcdPV_8p4UQY5x0ZFkknWxHYjgPPDY0RoeCoX5JPrVKz978JoPrKTAylL310GZB4XLEvf5CFPAgqi8xRoaL1.xmCUiBKKveBMeeV4Ch0no");   }
    public void UpdateCookies(string c)
    {
        c = c.Replace(";", ",");
        cookies = new CookieContainer();    
        cookies.SetCookies(new Uri("https://chat.openai.com"), c);
    }

    public async Task<bool> UpdateSession()
    {
        var handler = new HttpClientHandler();

        // If you are using .NET Core 3.0+ you can replace `~DecompressionMethods.None` to `DecompressionMethods.All`
        handler.AutomaticDecompression = ~DecompressionMethods.All;
   //     handler.CookieContainer = cookies;
        // In production code, don't destroy the HttpClient through using, but better use IHttpClientFactory factory or at least reuse an existing HttpClient instance
        // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests
        // https://www.aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
        using (var httpClient = new HttpClient(handler))
        {
            using (var request = new HttpRequestMessage(new HttpMethod("GET"), "https://chat.openai.com/api/auth/session"))
            {
                request.Headers.Add("authority", "chat.openai.com");
                request.Headers.Add("accept", "*/*");
                request.Headers.Add("accept-language", "en-US,en;q=0.9,es;q=0.8");
                request.Headers.Add("cookie", "intercom-device-id-dgkjq2bp=9ab266e7-104f-46df-a8ec-2726d512e855; __Host-next-auth.csrf-token=16594dd4956288954395ba48c00687e3641cc9e09a5e1483efcadcb0d254168e%7C3647ca9d35790a9d43b74f29a045c2fbbe749691582d7319289c55bc2dc02efb; ajs_user_id=nfC9zQAtFYO2fZHxuR2LN67NFtT2; ajs_anonymous_id=2a42d869-61bc-4339-9fdc-b4c6dde89a40; mp_d7d7628de9d5e6160010b84db960a7ee_mixpanel=%7B%22distinct_id%22%3A%20%22user-Dl4ifhTmjdWafGAXNhyWPGpr%22%2C%22%24device_id%22%3A%20%2217d39fbf4095ed-0ad3e62e84e519-978183a-144000-17d39fbf40a65e%22%2C%22%24search_engine%22%3A%20%22google%22%2C%22%24initial_referrer%22%3A%20%22https%3A%2F%2Fwww.google.com%2F%22%2C%22%24initial_referring_domain%22%3A%20%22www.google.com%22%2C%22%24user_id%22%3A%20%22user-Dl4ifhTmjdWafGAXNhyWPGpr%22%7D; __Secure-next-auth.callback-url=https%3A%2F%2Fchat.openai.com%2Fchat; cf_clearance=gEcuzFSxPtGLiQOiF5eEhjFse5Gi_SVeed6qwoP6IYU-1670827631-0-160; __Secure-next-auth.session-token=eyJhbGciOiJkaXIiLCJlbmMiOiJBMjU2R0NNIn0..6eZNpyxHdUsSULNn.QNuBEKKf_0srOQB4USS4Z4yBgFKvuWYTkMKqiZPFdhDhBAi9WwjvXcBOen9zfwcMEXs405dV3QZ-BBFFTJtlIVMbiLWZ6-AceO5PIrjff86bFtzOGGof8r6WMtBeq0p5FjPdLFgDlq1vorx47K6ycMP5aG4Dz4QTxYSgItfOBpOsYtMcuE9a3gnERXYkCiD1zoNGdtsltCwx-hnF7a0gRJ_4GaOV-9LsYcr0co-7KaIrvJMlymbHDcKH26vLmXfHpyENnoFe3awY9ubjV_JmD8y4k9DfyrxW1oFtHDEK-Pq5idEovBXkVpNdsgKDA4IxCYNVFu_uZlSlnNPoqNkNrTHd2Dxvgx5OoNaMmmUIxTpoeGfccyn9oBFQ5inkOEB4M_LUFRY1eb89ZrNl9lyx-yJmISVwy4YEVXLnBUdXumoG1DiuTzQe7HCZiUMv2vEj4qAspk7l8OpovzQ9KDh-wqPqKQhV3sluiherlqXdVuwdIQhNea6k6jtkZBkCwxB0vDi4c6cN72bJ5D92JinQQBieaEQH8dEOWw35Ga4T4XVss5rKWMPCaa3xAdJXDDxkE3-F4SNm08oRnCTwmB4osPSdorK0ET3drf6BQuh5pETAp4tSPjK4pXk6sHWAQnzNNK0U-I4D7sIP8VveacbdutNanEQE5OWtWb4fqGjvD2IwHC432tJ_8CaVPSTRVX-feSVshXte6hbRbu5OrkOTnb0H9YhlpwTKyV1cF1rJ7Su-qPadZ5ov1SS9Mg7nRql2Uhg5csnA7FIYqBWHPM4ML7MXhDJcApQ1c8NwVEmYX3VKQxNufQC4TNHzhIpxhHGhkGlB5k5rrOXAleD1rEvEA4G3pxitLlcYGCwTTxEqHrBGbvVrNgaLOtp3N0M8tECRILbSJw-u7hW7jGD4aV6N2_Rak3nws4gAe53hlvxJwVsA9gZ43kh6vwUoIp5TA7-mT-0r_j21QttXURQKirinNtVJ2IVYllIf3ZMKEUVX3C-3VporgL2_q6Cw01PRZSPqGXQ4p0Kvi06JtHBvhMT1p-DTKxALS_OdIfbJg4R0OSMILvNk6NkNt7EszT-wtW1Wvt_VQ87HTAjirvx1tr2pI85W6oh4QQd5XEvImAWJnPfWDnEIygupIsodOFIPwxtlWfh8LFy_6Va9GPCZ4ByCNIHuhkkHSPnv5d6FxZEnfZjRsYhVlPiRUHuJOuy6gjDh2vPUNMNbKXkXpHt9iq1BsDXIZmN_KdNUlm7dyFPFA-dWFPz4kWoH8UYmMKBPBCoWpDaTdFFaNY6c_LkKj-_nBt2h5YUOO3PB7k3JIrTRWfgPcCRZnsdVmb4D_pqeHaaU098-gnd6X6WLJWIbTEow6C3WdQnHrbbgTXMVjFcDSaPc3EoVOepMDZ-3ZOZjazmDhSw29w2a1mHsGmEs3WGanPKvYP0KSwac7Arc6R88BXvHqm8g5yKOYDy6--LmOzBIOhoaVlEN3AWveom2doSla2qalIEMDdFqcJwXRjicEypTLdM3GIfGt3vDsQ1l20JpQgG7YJAG_WcRbLqKfvwWoeIPZi89K_erm4C_WY6Xqia7j1Zb-uH93azFWoVwuKXTddoX7utPX2OYtPCYKH_QMLsQlGIJzUJtsh05S3PXwdjHea2qyRUCd89DH4qrlwf97LuWV4fx9fPqLl4TbesMVXqnoFKd-4FsPwM0HPBpRg0XWpedG1VjWy9B4hVbcyVhLVBGo4hMzJMrWrXbXh2e3gWzPgs3QxVVNtcWMI7j3gIx_Z2lGks55EWXXBIN9YXG6nGqhHFK5gNSUUzrHhsdrWrr-oaZAjUdp75ySOIxHxlon_VI4KUiXhCx55jbYxPLaSrViOO_EtABveaUw7mQaOIGvFJ_OrinidlquCnjKGKo5_C8-y-ADCiHDm7nN2Xv-cvPtQhbyx0yrpuzRjgV3ihivxo4kjPdu2fWvGSShpWtDgErQf1QX4mqcG7iAzla1Tbl0afpncMXfVjTGM856S9aeW2yv5MNBr8PUquxbq00UezWl8X5DkdfB9wXNAcWXp0ZbHAbtWOj9rHvsiMv2u0fw9MgLkPHbBTw58tJwkdbF0kyv9WWqNNY9SIrfIpcaabXRBx3462rVQRf8UToBU4KHhI8h-oK_vlRXdnZCD-V6ghvyjH1oMfhspBco_DTIY0DQEEbRVxRdI5bd73eL7IfF99mKCInWVJJ6W1qO2IjGfqFhzoxMuR3QtXa4nWcklN33FX6CuoyLx6EVhbTBW9G8R9F9otvvoBEIZmAiuTACXNsohl2njC_Mx49uwm_hn9rxX50kMCE_rcjeNAswl3K.XdLGlTMpyl72VIhLjtoAsA; __cf_bm=4w0T48HSuSnJvdMGmb5rHF_3RHn0mF8CX8zPEF.AsGw-1670827632-0-ATctwDzuy21gtLBWIEz6o5uwZjReeBuZWaJHatpGrtTs2vAWVZBDiw83gApHZ8EHm4nYxqv/taj5GNVsJ2Y7Y0I=");
                request.Headers.Add("referer", "https://chat.openai.com/chat");
                request.Headers.Add("sec-ch-ua", "\"Not?A_Brand\";v=\"8\", \"Chromium\";v=\"108\", \"Google Chrome\";v=\"108\"");
                request.Headers.Add("sec-ch-ua-mobile", "?1");
                request.Headers.Add("sec-ch-ua-platform", "\"Android\"");
                request.Headers.Add("sec-fetch-dest", "empty");
                request.Headers.Add("sec-fetch-mode", "cors");
                request.Headers.Add("sec-fetch-site", "same-origin");
                request.Headers.Add("user-agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Mobile Safari/537.36");

                try
                {
                    var response = await httpClient.SendAsync(request);
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                        return false;

                    var cooks = response.Headers.GetValues("set-cookie").ToList();//.Where(v=>v=="__Secure-next-auth.session-token").FirstOrDefault();
                    UpdateCookies(cooks[0]);
                    // cookie = cooks[0];
                  //  sessionToken = cooks[0].Substring(cooks[0].IndexOf("="));
                  //  sessionToken = sessionToken.Substring(0, sessionToken.IndexOf(";"));
                    var str = await response.Content.ReadAsStringAsync();
                    session = JsonConvert.DeserializeObject<Session>(str);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error refreshing session token", ex);
                    return false;
                }
                return true;
            }
        }
    }


    public async Task<string> Next(string msg)
    {
        // return "Failure";
        if (session == null)
        {
            var b =await UpdateSession();
            if (!b)
                return "Failure: Unauthorized";
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
        var txt = string.Join(" ",lastResponse.message.content.parts);//.Concat((a,b)=>a+b);//.Join(" ");
        if (txt.Contains("Unauthorized"))
            return "Failure " + txt;
        return txt;
    }
        
    

    
    /// <summary>
    /// Sends a chat and gets one back
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task<string> Next2(string msg)
    {
        int tries = 0;
        start:
        HttpClientHandler handler = new HttpClientHandler();
        handler.AutomaticDecompression = DecompressionMethods.All;
        handler.CookieContainer = cookies;
        HttpClient client = new HttpClient(handler);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://chat.openai.com/backend-api/conversation");

        request.Headers.Add("authority", "chat.openai.com");
        request.Headers.Add("accept", "text/event-stream");
        request.Headers.Add("accept-language", "en-US,en;q=0.9,es;q=0.8");
        request.Headers.Add("authorization", "Bearer " + session.accessToken);//.eyJodHRwczovL2FwaS5vcGVuYWkuY29tL3Byb2ZpbGUiOnsiZW1haWwiOiJib2JAZGFiYnIuY29tIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsImdlb2lwX2NvdW50cnkiOiJVUyJ9LCJodHRwczovL2FwaS5vcGVuYWkuY29tL2F1dGgiOnsidXNlcl9pZCI6InVzZXItRGw0aWZoVG1qZFdhZkdBWE5oeVdQR3ByIn0sImlzcyI6Imh0dHBzOi8vYXV0aDAub3BlbmFpLmNvbS8iLCJzdWIiOiJhdXRoMHw2MmY4NGM2NzRlNmExOGZkNmEwNmE1YzkiLCJhdWQiOlsiaHR0cHM6Ly9hcGkub3BlbmFpLmNvbS92MSIsImh0dHBzOi8vb3BlbmFpLmF1dGgwLmNvbS91c2VyaW5mbyJdLCJpYXQiOjE2NzA3NzA0NjUsImV4cCI6MTY3MDgxMzY2NSwiYXpwIjoiVGRKSWNiZTE2V29USHROOTVueXl3aDVFNHlPbzZJdEciLCJzY29wZSI6Im9wZW5pZCBlbWFpbCBwcm9maWxlIG1vZGVsLnJlYWQgbW9kZWwucmVxdWVzdCBvcmdhbml6YXRpb24ucmVhZCBvZmZsaW5lX2FjY2VzcyJ9.wqJQs2-HHrw3sb8PvZgYasQVInZkLebQyws_tVfB2V3N7EipxxMZeENYC8EPj-cCgdSopsbdxhcQ33WH-Jjsc6ja9TrQ9UvxJWdLXVRAmqjuwjmPeB20dzPZdBKqBDJj24L7XxUthTQrlhimR8tAQXHYaFNfl9LfzzDpdjs0SyPUTW3cU1dqJTEIg2OgaoCjg-R4ClsTLIGpy_Xe6686E6tMJ6Abi915Yy8_o66odi4HiWvgQDf7ghs8e9IURu9gM60ngRDIPhWO8LImrxmnWzfb7PrfAZPZY__y3vuuhS3euNnnJnImFRFwhA6DIoZD0uDnqfQRAWiwaaYQkHMUhQ");
      //  request.Headers.Add("cookie", "intercom-device-id-dgkjq2bp=9ab266e7-104f-46df-a8ec-2726d512e855; __Host-next-auth.csrf-token=16594dd4956288954395ba48c00687e3641cc9e09a5e1483efcadcb0d254168e%7C3647ca9d35790a9d43b74f29a045c2fbbe749691582d7319289c55bc2dc02efb; ajs_user_id=nfC9zQAtFYO2fZHxuR2LN67NFtT2; ajs_anonymous_id=2a42d869-61bc-4339-9fdc-b4c6dde89a40; mp_d7d7628de9d5e6160010b84db960a7ee_mixpanel=%7B%22distinct_id%22%3A%20%22user-Dl4ifhTmjdWafGAXNhyWPGpr%22%2C%22%24device_id%22%3A%20%2217d39fbf4095ed-0ad3e62e84e519-978183a-144000-17d39fbf40a65e%22%2C%22%24search_engine%22%3A%20%22google%22%2C%22%24initial_referrer%22%3A%20%22https%3A%2F%2Fwww.google.com%2F%22%2C%22%24initial_referring_domain%22%3A%20%22www.google.com%22%2C%22%24user_id%22%3A%20%22user-Dl4ifhTmjdWafGAXNhyWPGpr%22%7D; __Secure-next-auth.callback-url=https%3A%2F%2Fchat.openai.com%2Fchat; cf_clearance=_.2nmKYx4K_G9pvUAVDH9hpVclLqqhxrU2ZBGsqyiqI-1670793632-0-160; __Secure-next-auth.session-token=eyJhbGciOiJkaXIiLCJlbmMiOiJBMjU2R0NNIn0..Oj_g2Nq_dKahDtNM.W1m9OjSOHZAnGo7b1IOcd0ORRZYL1FlXpd7UHCd73JXb6yVNzYzWVa4GSmClUvAK8wEZt90YLLhmSrkXNWbuOYS8MkuvzcxPU70DFFX-dtZvuylkEvq58qkprNUV-HSOd7KWa9nWUqbwz0fQMcivsxhHCvSIWbdpymRc9JmPkDhQp4f45-6qb0kHwj-7DSHwCUYcWATVjQh0hQTWnCBqBOfvyX7Et_WEf53-MsAvv_3eBxT5NgLwuiLV-MuZPoGEOc7org5GCaCFnDdUDuI1ccpsyF4L2385Z-tLJmQ2BJ--58_u4p3CfnUS649l58BmtnGMQQuqAhSte8HnWFQSFlpqOmJZUdG13nS96_zYjclkn0uQuMUtbFxvz0wLzUR3VFzwnKdrT2qSR6kmeT8Qrgms7RtF7vkgfd69Iiv444LBmgNSWBouTynjxtOfQa0TDY85smSKqt7IEPlsMQ-9kfXzo9mstzu7rklUdm9PydzxXbFCTIIxo_58QzZJ9smWuj6-GgH3g_LQqDlWaM5ScHn3fqCljrKzvPGo0s1vxD1kKEBgd6jeBRPX53U8QDWUdiTCY6t1EUPyf9-30k66M5Mu3R8Wqsne1iO0pBiKReb996dAnRr-XatZUt93cRHjblDfneUKzE7DEYEcp8xrtT8FRwLBopkHAaj419mifUDDypR-LgZ81fuJ2HkMf76QK5t4RehGX9kVTNh-CWr_1Z7ETAAMVd4QWs5EbofIolcXTuiPiz8Juv_7eIsVhxed4LKqVRwIp348f1KTGMXQ-BTr0DVjkt49aFsvDwk12Vaj20kO_H3m5Fu3nN-uOVpFAYDJX767Iw-mvXNlYw5AODMcUpcOZTLQMt3IiZ49B_iYmGOynBWog4PMyHeV5FFRE1gUBE2PuZa2l-HwwruRq8XZbsj22iYk846ZGe4izSyaDBHxozKSOFUsJfHessgww5_YSnugPrf_5V6mh3g4mkqjzDpDGsVjoH3iZXQJJqKuKuLsd4lNH8nXPttVEIQukqIvgGJYnCtSODZ7ZSXWWQdFt3H1PwhD1r8iFaBSxcC3y5ojGLsAInDAxh-XTRBEmboDOmpEb0FwdwkfuahpMVnB1Eg82Qefn2qynDV9NpFcfcqFAw6Soq_JSLriT57cUX9JgcfxAe8RkmVO2ihWnfedElUEXnrtdUpZyhxi9sGBeEzSfw3dhlrB6q61wzpzcaCP-xoFngYv07LsnN4OF_2H2o20618nbqeJ8Tor9FwMJt63v9wUZm7vcBVBX5uqht-7QHwAmfHZqPZkgq7qaVlYmaad05AalN1h8x0OKS-6pOxt1WAqGTFYr8e5NqbaZQ6jGasMPRMPAU6DIt2EsjkrBtMctK-KHWaYmVV_fgEyaTghqkg7mUHwv98oim9ET-o3xDJATGRH0HlQTg2l-rg2rfHz17uJg6tw4ReGgQE1ehIUxApfHxGkA4DsQwXKwf4YqFp6D7j3n2X0D1nhRQp-tSTCynBhC7aEnsbwThKaKEvjkEm-OP1Xvx5TzsO4qi46K1EHePRrG5tI9clgWSfcNPm_-61B_LL8L2kfewfqe-8LnH3UVYXiTN6VxL_1nOfWkDB3Uyknb6BoRBm_lsZb_szRw6nFRDnEyNEZQMhGqb7U3-rddQvO9cgPv6HFfiIwkCCETTJ4352RXEnzme9-vrDyLa42cD6E5aXNbkwFDVveyEZjflCADivOO_PQeB9sVqiQ2ptTLfU9dWSVykMsqczH5awcZfepfpu0C7wSnTkSADtRToIVktUYox5S_hj39fVPO6uBXb3zNFMgX1tsVflKrMXUTwB6VTsRt21862KpffexXjbStGOplmh26tziSGbf7yZAdFB0UScXd3f7UYjeep0Dg_u6iOoPvB77t-8ZaqeY2e_1jmjJoQU91GwwukDJmD4uXSZPNdIWtJsFxGRpWjn-jpmu7oXohGyW0UfOumD9LrE7N1gEgyiD7IxXRrQKIEBXcGh-dl28JKND-otZRdc6IUNFmtHGljOZ0RsQJJYSSsgQr0MtaNcr3TENyAsBeyHpWm_21LUNZS04VwkqsEWeEQgFbu2Q2MS6c-1XbGjAAhkPNM7a9WnNHoM4S4tif1qLockFQV8Hhlxpt9dy9JUQh9ilzS2hHuljxrDA-DNtnt0yvNHLzTeMpCelrlbg2B3_uA_UUUNsNTSTGkCLRE3lN2CTFelOCFaiGuxoeAUxscq1Pvh9vbWfSvxx7OtUu7GzI9BrFLSAxLBalE_lHr-ut39pC8QSVANmNCvcG8kAnESq4cRrIqlMu3CroL7u0GaiRlMhd5xEISSj.uDgH2DVyp-y2K8PwVwXS6g; __cf_bm=DDbEiW6Fb48VFYR2rMKj29_gWOuZyIWvq_1cfivU4y4-1670794480-0-AYDiAAsxtABFQ+si2wfkunfiyvKhhbTqsiCuXy5F1BnO974xzLYhmRcSpcH4wI3WsL8b+KJYVx5pk9tTIis1YFM=");
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
        if(!response.IsSuccessStatusCode)
        {
            await UpdateSession();
            tries++;
            if (tries > 1) return response.ReasonPhrase;
            goto start;
        }
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

