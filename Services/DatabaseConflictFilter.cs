using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace code.Services;

public sealed class DatabaseConflictFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is DbUpdateException { InnerException: Microsoft.Data.SqlClient.SqlException sql })
        {
            if (sql.Number == 51000)
            {
                context.Result = new BadRequestObjectResult(sql.Message);
                context.ExceptionHandled = true;
                return;
            }
            if (sql.Number is 2601 or 2627 or 1205)
            {
                context.Result = new ConflictObjectResult("Dữ liệu vừa thay đổi hoặc bị trùng. Hãy tải lại trang và thử lại.");
                context.ExceptionHandled = true;
                return;
            }
        }
        if (context.Exception is not DbUpdateConcurrencyException) return;
        context.Result = new ConflictObjectResult("Dữ liệu vừa được xử lý bởi người khác. Hãy tải lại trang và kiểm tra trạng thái trước khi tiếp tục.");
        context.ExceptionHandled = true;
    }
}
