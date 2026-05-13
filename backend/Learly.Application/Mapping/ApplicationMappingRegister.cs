using Learly.Application.Contracts.Aulas.Requests;
using Learly.Application.Contracts.Aulas.Responses;
using Learly.Application.Contracts.Calendario.Responses;
using Learly.Application.Contracts.Escolas.Responses;
using Learly.Application.Contracts.Matriculas.Responses;
using Learly.Domain.Entities;
using Learly.Domain.ReadModels;
using Mapster;

namespace Learly.Application.Mapping;

public sealed class ApplicationMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Escola, EscolaListItemResponse>();

        config.NewConfig<Aula, AulaListItemResponse>();
        config.NewConfig<CalendarioGeral, EventoCalendarioResponse>();

        config.NewConfig<MatriculaListagemItem, MatriculaListItemResponse>();

        config.NewConfig<CriarAulaRequest, Aula>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.EscolaId)
            .Ignore(dest => dest.Status)
            .Map(dest => dest.TipoAula, src => src.TipoAula ?? "Normal");
    }
}
