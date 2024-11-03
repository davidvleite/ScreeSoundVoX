using System.Net.Http.Json;
using ScreenSound.Web.Response;

namespace ScreenSound.Web.Services
{
    public class AuthAPI(IHttpClientFactory factory)
    {
        private readonly HttpClient _httpClient = factory.CreateClient("API");
        //auth/login?useCookies=true
        public async Task<AuthResponse> LoginAsync(string email, string senha)
        {
            var response = await _httpClient.PostAsJsonAsync("auth/login?useCookies=true", new
            {
                email,
                password = senha
            });

            if (response.IsSuccessStatusCode)
            {
                return new AuthResponse { Sucesso = true };

            }
            return new AuthResponse { Sucesso = false, Erros = ["Login/Senha inválidos"]};
        }
    }
}