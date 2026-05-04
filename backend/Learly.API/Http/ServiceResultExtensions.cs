using Learly.Application.Contracts.Aulas;
using Learly.Application.Contracts.Aulas.Responses;
using Learly.Application.Contracts.Alunos.Responses;
using Learly.Application.Contracts.Escolas;
using Learly.Application.Contracts.Escolas.Responses;
using Learly.Application.Contracts.Matriculas;
using Learly.Application.Contracts.Matriculas.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Learly.API.Http;

public static class ServiceResultExtensions
{
    public static IActionResult ToActionResult(this EscolaCriacaoResultado r, ControllerBase c)
    {
        if (r.Ok && r.Escola is not null)
        {
            return c.StatusCode(StatusCodes.Status201Created, r.Escola);
        }

        return r.Falha switch
        {
            EscolaCriacaoFalha.CodigoDuplicado or EscolaCriacaoFalha.EmailDuplicado => c.Conflict(new ProblemDetails
            {
                Title = "Conflito",
                Detail = r.Mensagem,
                Status = StatusCodes.Status409Conflict
            }),
            EscolaCriacaoFalha.CodigoReservado or EscolaCriacaoFalha.Validacao => c.BadRequest(new ProblemDetails
            {
                Title = "Requisicao invalida",
                Detail = r.Mensagem,
                Status = StatusCodes.Status400BadRequest
            }),
            _ => c.BadRequest(new ProblemDetails
            {
                Title = "Requisicao invalida",
                Detail = r.Mensagem ?? "Falha ao criar escola.",
                Status = StatusCodes.Status400BadRequest
            })
        };
    }

    public static IActionResult ToActionResult(this AulaCriacaoResultado r, ControllerBase c)
    {
        if (r.Ok && r.Id.HasValue)
        {
            return c.StatusCode(StatusCodes.Status201Created, new CriarAulaResponse(r.Id.Value));
        }

        if (r.Falha == AulaCriacaoFalha.AcessoNegado)
        {
            return c.Forbid();
        }

        return c.BadRequest(new ProblemDetails
        {
            Title = "Requisicao invalida",
            Detail = r.Mensagem ?? "Falha ao criar aula.",
            Status = StatusCodes.Status400BadRequest
        });
    }

    public static IActionResult ToActionResult(this AulaOperacaoResultado r, ControllerBase c, string fallbackTitle)
    {
        if (r.Ok)
        {
            return c.NoContent();
        }

        var code = r.StatusCode is >= 400 and <= 599 ? r.StatusCode : StatusCodes.Status400BadRequest;
        var title = code switch
        {
            StatusCodes.Status403Forbidden => "Acesso negado",
            StatusCodes.Status404NotFound => "Nao encontrado",
            StatusCodes.Status409Conflict => "Conflito",
            _ => "Requisicao invalida"
        };

        return c.StatusCode(code, new ProblemDetails
        {
            Title = title,
            Detail = r.Mensagem ?? fallbackTitle,
            Status = code
        });
    }

    public static IActionResult ToActionResult(this MatriculaCriacaoResultado r, ControllerBase c)
    {
        if (r.Ok && r.Id.HasValue)
        {
            return c.StatusCode(StatusCodes.Status201Created, new CriarMatriculaResponse(r.Id.Value));
        }

        if (r.Falha == MatriculaCriacaoFalha.AcessoNegado)
        {
            return c.Forbid();
        }

        if (r.Falha == MatriculaCriacaoFalha.Conflito)
        {
            return c.Conflict(new ProblemDetails
            {
                Title = "Conflito",
                Detail = r.Mensagem ?? "Falha ao criar matricula.",
                Status = StatusCodes.Status409Conflict
            });
        }

        return c.BadRequest(new ProblemDetails
        {
            Title = "Requisicao invalida",
            Detail = r.Mensagem ?? "Falha ao criar matricula.",
            Status = StatusCodes.Status400BadRequest
        });
    }

    public static IActionResult ToActionResult(this MatriculaOperacaoResultado r, ControllerBase c, string fallbackTitle)
    {
        if (r.Ok)
        {
            return c.NoContent();
        }

        var code = r.StatusCode is >= 400 and <= 599 ? r.StatusCode : StatusCodes.Status400BadRequest;
        var title = code switch
        {
            StatusCodes.Status403Forbidden => "Acesso negado",
            StatusCodes.Status404NotFound => "Nao encontrado",
            StatusCodes.Status409Conflict => "Conflito",
            _ => "Requisicao invalida"
        };

        return c.StatusCode(code, new ProblemDetails
        {
            Title = title,
            Detail = r.Mensagem ?? fallbackTitle,
            Status = code
        });
    }

    public static IActionResult ToActionResult(this MatriculaListagemResultado r, ControllerBase c)
    {
        if (r.Ok)
        {
            return c.Ok(r.Itens);
        }

        if (r.Falha == MatriculaListagemFalha.AcessoNegado)
        {
            return c.Forbid();
        }

        return c.BadRequest(new ProblemDetails
        {
            Title = "Requisicao invalida",
            Detail = r.Mensagem ?? "Falha ao listar matriculas.",
            Status = StatusCodes.Status400BadRequest
        });
    }

    public static IActionResult ToActionResult(this CriarAlunoResultado r, ControllerBase c)
    {
        if (r.Ok && r.AlunoId.HasValue && r.MatriculaId.HasValue)
        {
            return c.StatusCode(
                StatusCodes.Status201Created,
                new CriarAlunoResponse(r.AlunoId.Value, r.MatriculaId.Value));
        }

        if (r.Falha == CriarAlunoFalha.AcessoNegado)
        {
            return c.Forbid();
        }

        if (r.Falha == CriarAlunoFalha.Conflito)
        {
            return c.Conflict(new ProblemDetails
            {
                Title = "Conflito",
                Detail = r.Mensagem ?? "Falha ao criar aluno.",
                Status = StatusCodes.Status409Conflict
            });
        }

        return c.BadRequest(new ProblemDetails
        {
            Title = "Requisicao invalida",
            Detail = r.Mensagem ?? "Falha ao criar aluno.",
            Status = StatusCodes.Status400BadRequest
        });
    }
}
