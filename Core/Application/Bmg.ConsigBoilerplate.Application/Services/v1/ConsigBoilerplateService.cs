using Bmg.Project.Utils.Base;
using Bmg.Project.Utils.Data;
using Bmg.ConsigBoilerplate.Database.Entities.v1;
using Bmg.ConsigBoilerplate.Database.UnitOfWork.Interfaces.v1;
using Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.FaceTec.v1;
using Bmg.ConsigBoilerplate.Domain.Models.v1;
using Bmg.ConsigBoilerplate.Domain.Services.v1;
using Microsoft.AspNetCore.JsonPatch;
using System.Transactions;

namespace Bmg.ConsigBoilerplate.Application.Services.v1
{
    [BmgDynatraceTrace]
    public class ConsigBoilerplateService : BmgServiceBase, IConsigBoilerplateService
    {
        private readonly IUnitOfWorkOracle _unitOfWork;
        private readonly IFaceTecApiManager _faceTecApiManager;

        public ConsigBoilerplateService
        (
            IUnitOfWorkOracle unitOfWork,
            IFaceTecApiManager faceTecApiManager
        )
        {
            _unitOfWork = unitOfWork;
            _faceTecApiManager = faceTecApiManager;
        }

        public async Task<IEnumerable<WeatherModel>> GetWeathersAsync(CancellationToken cancellationToken)
        {
            var result = await _unitOfWork.Weathers.SelectAsync(cancellationToken);

            return Mapper.Map<IEnumerable<WeatherModel>>(result);
        }

        public async Task<PaginatedData<WeatherModel>> GetWeathersPaginatedAsync(int pageSize, int pageNumber, CancellationToken cancellationToken)
        {
            var result = await _unitOfWork.Weathers.SelectPaginationAsync(pageSize, pageNumber, cancellationToken);

            return Mapper.Map<PaginatedData<WeatherModel>>(result);
        }

        public async Task<WeatherModel> GetWeatherAsync(long id, CancellationToken cancellationToken)
        {
            var result = await _unitOfWork.Weathers.SelectAsync(cancellationToken, id);

            return Mapper.Map<WeatherModel>(result);
        }

        // TODO: EXEMPLO DE CHAMADA DE API'S, GERAÇÃO DE NOTIFICAÇÕES E TRANSAÇÃO NA BASE DE DADOS
        public async Task<WeatherModel> CreateWeatherAsync(WeatherModel weather, CancellationToken cancellationToken)
        {
            // TODO: EXEMPLO DE TRANSACAO NO BANCO DE DADOS, OS REPOSITORYS (IDBCONNECTION) UTILIZADOS DENTRO DO BLOCO DE CHAVES ESTARÃO NA MESMA TRANSAÇÃO E AGUARDANDO O COMMIT OU ROLLBACK
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                //_faceTecApiManager.SetCancellationToken(cancellationToken);

                //var autenticacaoFaceTec = await _faceTecApiManager.AuthenticateAsync(new AuthenticationRequest
                //{
                //    Username = "Teste",
                //    Password = "Teste"
                //});

                // TODO: EXEMPLO DO ENVIO DE NOTIFICAÇÕES NO RETORNO DA APLICAÇÃO (CRIADO PARA EVITAR O USO DE EXCEPTIONS COMO RETORNO E QUEBRA DO CÓDIGO
                //if (weather.Summary.Equals("NotifyTest", StringComparison.OrdinalIgnoreCase))
                //{
                //    await Notifier.NotifyAsync(nameof(Weather), "Summary cannot be NotifyTest");
                //    return null;
                //}

                var result = weather with { Id = weather.Id };

                // TODO: AO CHAMAR O COMPLETE VOCÊ INDICA QUE O IDBCONNECTION DEVERÁ FAZER O COMMIT DA TRANSAÇÃO, CASO OCORRA UMA EXCEÇÃO ANTES DESSA CHAMADA E SAIA DO BLOCO DE CHAVES O SISTEMA IRÁ ENTENDER E REALIZAR UM ROLLBACK
                transaction.Complete();

                return result;
            }
        }

        public async Task<bool> PatchWeatherAsync(long id, JsonPatchDocument<WeatherModel> weatherPatch, CancellationToken cancellationToken)
        {
            var weather = await this.GetWeatherAsync(id, cancellationToken);

            if (weather == null)
                return false;

            weatherPatch.ApplyTo(weather);

            var result = await _unitOfWork.Weathers.UpdateAsync(Mapper.Map<Weather>(weather), cancellationToken);

            return result;
        }

        public async Task<bool> UpdateWeatherAsync(WeatherModel weather, CancellationToken cancellationToken)
        {
            var weatherResult = await this.GetWeatherAsync(weather.Id, cancellationToken);

            if (weatherResult == null)
                return false;

            var result = await _unitOfWork.Weathers.UpdateAsync(Mapper.Map<Weather>(weather), cancellationToken);

            return result;
        }

        public async Task<bool> DeleteWeatherAsync(long id, CancellationToken cancellationToken)
        {
            var weather = await this.GetWeatherAsync(id, cancellationToken);

            if (weather == null)
                return false;

            var result = await _unitOfWork.Weathers.DeleteAsync(Mapper.Map<Weather>(weather), cancellationToken);

            return result;
        }
    }
}
