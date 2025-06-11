using SecureLink.Shared.Models;

namespace SecureLink.Api.Data.Interfaces
{
    public interface IKeyAssigmentDataAccess
    {
        Task InsertKeyAssigmentsAsync(KeyAssigment assigment);
    }
}
