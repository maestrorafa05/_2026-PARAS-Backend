using PARAS.Api.Domain.Enums;

namespace PARAS.Api.DTOs;

public record ChangeLoanStatusRequest(
    LoanStatus ToStatus,
    string? Comment
);

public record LoanStatusHistoryResponse(
    Guid Id,
    Guid LoanId,
    LoanStatus FromStatus,
    LoanStatus ToStatus,
    string? ChangedBy,
    string? Comment,
    DateTime ChangedAt
);