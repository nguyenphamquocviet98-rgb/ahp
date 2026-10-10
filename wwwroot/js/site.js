// ============================================================
// site.js — Tiện ích dùng chung toàn ứng dụng
// ============================================================

// Hiện modal xác nhận xóa (partial _DeleteModal phải có trên trang).
// id: khóa chính; label: tên/mã hiển thị trong câu hỏi xác nhận.
function confirmDelete(id, label) {
    const idInput = document.getElementById('deleteRecordId');
    const labelEl = document.getElementById('deleteRecordLabel');
    const modalEl = document.getElementById('deleteModal');
    if (!idInput || !modalEl) return;

    idInput.value = id;
    if (labelEl) labelEl.textContent = label || '';
    bootstrap.Modal.getOrCreateInstance(modalEl).show();
}
