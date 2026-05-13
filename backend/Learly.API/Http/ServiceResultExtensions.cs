using Learly.Application.Contracts.Aulas;
using Learly.Application.Contracts.Aulas.Responses;
using Learly.Application.Contracts.Alunos.Responses;
using Learly.Application.Contracts.Escolas;
using Learly.Application.Contracts.Escolas.Responses;
using Learly.Application.Contracts.HorariosFuncionamento;
using Learly.Application.Contracts.Matriculas;
using Learly.Application.Contracts.Matriculas.Responses;
using Learly.Application.Contracts.Livros;
using Learly.Application.Contracts.PreAlunos;
using Learly.Application.Contracts.PreAlunos.Responses;
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

    public static IActionResult ToActionResult(this PreAlunosCatalogoLivrosResultado r, ControllerBase c)
    {
        if (r.Ok)
            return c.Ok(r.Itens);

        return r.Falha switch
        {
            PreAlunosCatalogoLivrosFalha.AcessoNegado => c.Forbid(),
            _ => c.BadRequest(new ProblemDetails
            {
                Title = "Requisicao invalida",
                Detail = r.Mensagem ?? "Falha ao listar livros.",
                Status = StatusCodes.Status400BadRequest
            })
        };
    }

    public static IActionResult ToActionResult(this PreAlunosListagemResultado r, ControllerBase c)
    {
        if (r.Ok)
            return c.Ok(r.Itens);

        if (r.Falha == PreAlunosListagemFalha.AcessoNegado)
            return c.Forbid();

        return c.BadRequest(new ProblemDetails
        {
            Title = "Requisicao invalida",
            Detail = r.Mensagem ?? "Falha ao listar pre-alunos.",
            Status = StatusCodes.Status400BadRequest
        });
    }

    public static IActionResult ToActionResult(this PreAlunoDetalheResultado r, ControllerBase c)
    {
        if (r.Ok && r.Detalhe is not null)
            return c.Ok(r.Detalhe);

        return r.Falha switch
        {
            PreAlunoDetalheFalha.AcessoNegado => c.Forbid(),
            PreAlunoDetalheFalha.NaoEncontrado => c.NotFound(new ProblemDetails
            {
                Title = "Nao encontrado",
                Detail = r.Mensagem ?? "Pre-aluno nao encontrado.",
                Status = StatusCodes.Status404NotFound
            }),
            _ => c.BadRequest(new ProblemDetails
            {
                Title = "Requisicao invalida",
                Detail = r.Mensagem ?? "Falha ao obter pre-aluno.",
                Status = StatusCodes.Status400BadRequest
            })
        };
    }

    public static IActionResult ToActionResult(this PreAlunoCriacaoResultado r, ControllerBase c)
    {
        if (r.Ok && r.Id.HasValue)
            return c.StatusCode(StatusCodes.Status201Created, new CriarPreAlunoResponse(r.Id.Value));

        if (r.Falha == PreAlunoCriacaoFalha.AcessoNegado)
            return c.Forbid();

        if (r.Falha == PreAlunoCriacaoFalha.Conflito)
        {
            return c.Conflict(new ProblemDetails
            {
                Title = "Conflito",
                Detail = r.Mensagem ?? "Falha ao criar pre-aluno.",
                Status = StatusCodes.Status409Conflict
            });
        }

        return c.BadRequest(new ProblemDetails
        {
            Title = "Requisicao invalida",
            Detail = r.Mensagem ?? "Falha ao criar pre-aluno.",
            Status = StatusCodes.Status400BadRequest
        });
    }

    public static IActionResult ToActionResult(this LivrosEscolaListagemResultado r, ControllerBase c)
    {
        if (r.Ok)
            return c.Ok(r.Itens);

        if (r.Falha == LivrosEscolaFalha.AcessoNegado)
            return c.Forbid();

        return c.BadRequest(new ProblemDetails
        {
            Title = "Requisicao invalida",
            Detail = r.Mensagem ?? "Falha ao listar livros.",
            Status = StatusCodes.Status400BadRequest
        });
    }

    public static IActionResult ToActionResult(this LivrosEscolaDetalheResultado r, ControllerBase c)
    {
        if (r.Ok && r.Livro is not null)
            return c.Ok(r.Livro);

        return r.Falha switch
        {
            LivrosEscolaFalha.AcessoNegado => c.Forbid(),
            LivrosEscolaFalha.NaoEncontrado => c.NotFound(new ProblemDetails
            {
                Title = "Nao encontrado",
                Detail = r.Mensagem ?? "Livro nao encontrado.",
                Status = StatusCodes.Status404NotFound
            }),
            _ => c.BadRequest(new ProblemDetails
            {
                Title = "Requisicao invalida",
                Detail = r.Mensagem ?? "Falha ao obter livro.",
                Status = StatusCodes.Status400BadRequest
            })
        };
    }

    public static IActionResult ToActionResult(this LivrosEscolaCriacaoResultado r, ControllerBase c)
    {
        if (r.Ok && r.Livro is not null)
        {
            return c.StatusCode(StatusCodes.Status201Created, r.Livro);
        }

        if (r.Falha == LivrosEscolaFalha.AcessoNegado)
            return c.Forbid();

        if (r.Falha == LivrosEscolaFalha.Conflito)
        {
            return c.Conflict(new ProblemDetails
            {
                Title = "Conflito",
                Detail = r.Mensagem ?? "Falha ao criar livro.",
                Status = StatusCodes.Status409Conflict
            });
        }

        return c.BadRequest(new ProblemDetails
        {
            Title = "Requisicao invalida",
            Detail = r.Mensagem ?? "Falha ao criar livro.",
            Status = StatusCodes.Status400BadRequest
        });
    }

    public static IActionResult ToActionResult(this LivrosEscolaAtualizacaoResultado r, ControllerBase c)
    {
        if (r.Ok && r.Livro is not null)
            return c.Ok(r.Livro);

        if (r.Falha == LivrosEscolaFalha.AcessoNegado)
            return c.Forbid();

        if (r.Falha == LivrosEscolaFalha.NaoEncontrado)
        {
            return c.NotFound(new ProblemDetails
            {
                Title = "Nao encontrado",
                Detail = r.Mensagem ?? "Livro nao encontrado.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (r.Falha == LivrosEscolaFalha.Conflito)
        {
            return c.Conflict(new ProblemDetails
            {
                Title = "Conflito",
                Detail = r.Mensagem ?? "Falha ao atualizar livro.",
                Status = StatusCodes.Status409Conflict
            });
        }

        return c.BadRequest(new ProblemDetails
        {
            Title = "Requisicao invalida",
            Detail = r.Mensagem ?? "Falha ao atualizar livro.",
            Status = StatusCodes.Status400BadRequest
        });
    }

    public static IActionResult ToActionResult(this HorariosFuncionamentoListagemResultado r, ControllerBase c)
    {
        if (r.Ok)
            return c.Ok(r.Itens);

        if (r.Falha == HorariosFuncionamentoFalha.AcessoNegado)
            return c.Forbid();

        return c.BadRequest(new ProblemDetails
        {
            Title = "Requisicao invalida",
            Detail = r.Mensagem ?? "Falha ao listar horarios.",
            Status = StatusCodes.Status400BadRequest
        });
    }

    public static IActionResult ToActionResult(this HorariosFuncionamentoAtualizacaoResultado r, ControllerBase c)
    {
        if (r.Ok && r.Itens is not null)
            return c.Ok(r.Itens);

        if (r.Falha == HorariosFuncionamentoFalha.AcessoNegado)
            return c.Forbid();

        return c.BadRequest(new ProblemDetails
        {
            Title = "Requisicao invalida",
            Detail = r.Mensagem ?? "Falha ao atualizar horarios.",
            Status = StatusCodes.Status400BadRequest
        });
    }

    public static IActionResult ToActionResult(this PreAlunoOperacaoResultado r, ControllerBase c, string fallbackTitle)
    {
        if (r.Ok)
            return c.NoContent();

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
}
