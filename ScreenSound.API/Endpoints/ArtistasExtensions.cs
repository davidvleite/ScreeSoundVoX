﻿
using Microsoft.AspNetCore.Mvc;
using ScreenSound.Banco;
using ScreenSound.Modelos;
using ScreenSound.API.Requests;
using ScreenSound.API.Response;
using ScreenSound.API.Request;
using System.Security.Claims;
using ScreenSound.Shared.Dados.Modelos;

namespace ScreenSound.API.Endpoints
{
    public static class ArtistasExtensions
    {
        public static void AddEndPointsArtistas(this WebApplication app)
        {
            var groupBuilder = app.MapGroup("artistas")
                .RequireAuthorization()
                .WithTags("Artistas");


            #region Artistas
            groupBuilder.MapGet("", ([FromServices] DAL<Artista> dal) =>
            {
                var listaDeArtistas = dal.Listar();
                if (listaDeArtistas is null)
                {
                    return Results.NotFound();
                }
                var listaDeArtistaResponse = EntityListToResponseList(listaDeArtistas);
                return Results.Ok(listaDeArtistaResponse);
            });

            groupBuilder.MapGet("{nome}", ([FromServices] DAL<Artista> dal, string nome) =>
            {
                var artista = dal.RecuperarPor(a => a.Nome.ToUpper().Equals(nome.ToUpper()));
                if (artista is null)
                {
                    return Results.NotFound();
                }
                return Results.Ok(EntityToResponse(artista));
            });

            groupBuilder.MapPost("", async ([FromServices] IHostEnvironment env, [FromServices] DAL<Artista> dal, [FromBody] ArtistaRequest artistaRequest) =>
            {
                var nome = artistaRequest.nome.Trim();
                var imagemArtista = DateTime.Now.ToString("ddMMyyyyhhss") + "." + nome + ".jpeg";
                var path = Path.Combine(env.ContentRootPath, "wwwroot", "FotosPerfil", imagemArtista);
                using MemoryStream ms = new MemoryStream(Convert.FromBase64String(artistaRequest.fotoPerfil!));
                using FileStream fs = new(path, FileMode.Create);
                await ms.CopyToAsync(fs);
                var artista = new Artista(artistaRequest.nome, artistaRequest.bio)
                {
                    FotoPerfil = $"/FotoPerfil/{imagemArtista}"
                };

                dal.Adicionar(artista);
                return Results.Ok();
            });

            groupBuilder.MapDelete("{id}", ([FromServices] DAL<Artista> dal, int id) =>
            {
                var artista = dal.RecuperarPor(a => a.Id == id);
                if (artista is null)
                {
                    return Results.NotFound();
                }
                dal.Deletar(artista);
                return Results.NoContent();
            });

            groupBuilder.MapPut("", ([FromServices] DAL<Artista> dal, [FromBody] ArtistaRequestEdit artistaRequestEdit) =>
            {
                var artistaAtualizar = dal.RecuperarPor(a => a.Id == artistaRequestEdit.Id);
                if (artistaAtualizar is null)
                {
                    return Results.NotFound();
                }
                artistaAtualizar.Nome = artistaRequestEdit.nome;
                artistaAtualizar.Bio = artistaRequestEdit.bio;
                dal.Atualizar(artistaAtualizar);
                return Results.Ok();
            });

            groupBuilder.MapPost("avaliacao", (HttpContext context, [FromBody] AvaliacaoArtistaRequest request, [FromServices] DAL<Artista> dal, [FromServices] DAL<PessoaComAcesso> dalPessoa) =>
            {
                var artista = dal.RecuperarPor(a => a.Id == request.ArtistaId);

                if (artista is null)
                {
                    return Results.NotFound();
                }

                var email = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? throw new InvalidOperationException("Pessoa não está conectada!");

                var pessoa = dalPessoa.RecuperarPor(p => p.Email.Equals(email)) ?? throw new InvalidOperationException("Pessoa não está conectada!");

                var avaliacao = artista.Avaliacoes.FirstOrDefault(a => a.ArtistaId == artista.Id && a.PessoaId == pessoa.Id);

                if (avaliacao is null)
                {
                    artista.AdicionarNota(pessoa.Id, request.Nota);

                }
                else
                {
                    avaliacao.Nota = request.Nota;
                }

                dal.Atualizar(artista);

                return Results.Created();
            });

            groupBuilder.MapGet("{id}/avaliacao", (int id, HttpContext context, [FromServices] DAL<Artista> dalArtista, [FromServices] DAL<PessoaComAcesso> dalPessoa
            ) =>
                    {
                        var artista = dalArtista.RecuperarPor(a => a.Id == id);

                        if (artista is null) 
                        {
                            return Results.NotFound();
                        }
                        var email = context.User.Claims
                            .FirstOrDefault(c => c.Type.Equals(ClaimTypes.Email))?.Value
                            ?? throw new InvalidOperationException("Não foi encontrado o email da pessoa logada");

                        var pessoa = dalPessoa.RecuperarPor(p => p.Email!.Equals(email))
                            ?? throw new InvalidOperationException("Não foi encontrado o email da pessoa logada");

                        var avaliacao = artista
                            .Avaliacoes
                            .FirstOrDefault(a => a.ArtistaId == id && a.PessoaId == pessoa.Id);

                        if (avaliacao is null)
                        {
                            return Results.Ok(new AvaliacaoArtistaResponse(id, 0));
                        }
                        else
                        {
                            return Results.Ok(new AvaliacaoArtistaResponse(id, avaliacao.Nota));
                        } 
                    });
            #endregion
        }
        private static ICollection<ArtistaResponse> EntityListToResponseList(IEnumerable<Artista> listaDeArtistas)
        {
            return listaDeArtistas.Select(a => EntityToResponse(a)).ToList();
        }

        private static ArtistaResponse EntityToResponse(Artista artista)
        {
            return new ArtistaResponse(artista.Id, artista.Nome, artista.Bio, artista.FotoPerfil);
        }
    }
}
