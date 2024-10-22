using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NIJO_APPLICATION_TEMPLATE {
    /// <summary>
    /// クライアント側からアップロードされたファイルをサーバー側のストレージに保存します。
    /// </summary>
    public class SavingUploadedFilesFilter : IAsyncActionFilter {
        public SavingUploadedFilesFilter(IFileAttachmentRepository attachmentRepository) {
            _attachmentRepository = attachmentRepository;
        }
        private readonly IFileAttachmentRepository _attachmentRepository;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {

            // ファイルを添付することができるContent-Typeでない場合は処理中断
            if (!context.HttpContext.Request.HasFormContentType) return;

            foreach (var file in context.HttpContext.Request.Form.Files) {

                // 入力エラーチェック
                if (string.IsNullOrWhiteSpace(file.Name)) {
                    context.Result = new BadRequestObjectResult($"ファイル '{file.FileName}' のName属性が指定されていません。");
                    return;
                }

                var errors = new List<string>();
                var id = new FileAttachmentId(file.Name);
                using var stream = file.OpenReadStream();
                await _attachmentRepository.SaveFileAsync(id, file.FileName, stream, errors);

                if (errors.Count > 0) {
                    context.Result = new BadRequestObjectResult(string.Join(Environment.NewLine, errors));
                    return;
                }
            }
        }
    }
}
