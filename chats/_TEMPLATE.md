# 📖 CÁCH DÙNG TÀI LIỆU NÀY (đọc 30 giây là dùng được)

> File bàn giao 1 phiên làm việc với AI. Quy ước: mỗi phiên = 1 file `chats/<ngày>-<chủ đề chính>.md`.
> File mới nhất (theo ngày) = trạng thái hiện tại của dự án. **Nhìn tên file là biết phiên đó làm gì.**
>
> ▶️ **CÁCH DÙNG MẪU NÀY:** copy file này thành `chats/<ngày>-<chủ đề>.md` rồi điền 4 mục bên dưới
> (hoặc bảo AI: *"Viết tổng kết phiên này vào `chats/<ngày>-<chủ đề>.md` theo mẫu `_TEMPLATE.md`"*).

**🟢 NGƯỜI NHẬN VIỆC (đầu phiên):**
1. Lấy bản mới nhất: `git checkout master && git pull`
2. Mở Kiro trong thư mục dự án: `kiro-cli chat`
3. Câu **ĐẦU TIÊN** gõ cho AI:
   > *"Đọc file `chats/<file mới nhất>.md` rồi tiếp tục từ mục '➡️ CẦN LÀM TIẾP'."*

**🔵 NGƯỜI BÀN GIAO (cuối phiên):**
1. Bảo AI viết tổng kết vào `chats/<ngày>-<chủ đề>.md` theo mẫu này.
2. Đẩy lên GitHub:
   ```bash
   git add chats/
   git commit -m "chat <ngày>: <tóm tắt ngắn>"
   git push origin master
   ```

**(Tùy chọn)** lưu nguyên văn hội thoại để AI đọc lại: `/chat save chats/<ngày>-session.json`, phiên sau `/chat load <file đó>`.

---

# Phiên <YYYY-MM-DD> — <Chủ đề chính> — <Tên người làm>

> **Dự án / bối cảnh ngắn:** <1–2 dòng cho người lần đầu đọc>

---

## ✅ ĐÃ LÀM XONG
- <việc đã hoàn thành 1>
- <việc đã hoàn thành 2>

## 🔧 ĐANG LÀM DỞ / CHƯA XONG
- <việc đang dở, đang ở bước nào>

## ➡️ CẦN LÀM TIẾP (việc cho người sau)
1. <việc tiếp theo, ưu tiên cao trước>
2. <...>

## ⚠️ LƯU Ý / CẠM BẪY / THÔNG TIN CẦN BIẾT
- <thông tin quan trọng: URL, đường dẫn, mật khẩu (đừng commit secret!), lỗi đã gặp & cách xử lý>
- <quyết định kỹ thuật & lý do>

---

<!-- (Tùy chọn) Thêm mục "CHI TIẾT KỸ THUẬT" bên dưới nếu phiên có nhiều thông tin tra cứu sâu:
     kiến trúc, bảng địa chỉ/cấu hình, lệnh vận hành, sơ đồ... -->
