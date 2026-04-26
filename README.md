**#  Unity 2D Team Workflow (Git + GitHub)**

Tài liệu này giúp team làm việc chung trên project Unity bằng Git mà không bị lỗi hoặc conflict.

---

**#  Cấu trúc project (QUAN TRỌNG)**

Chỉ được push các thư mục sau:

```
Assets/
ProjectSettings/
Packages/
```

KHÔNG BAO GIỜ push:

```
Library/
Temp/
Logs/
Build/
```

---

#  QUY TRÌNH LÀM VIỆC MỖI NGÀY

##  1. Mở project → cập nhật code mới nhất

```bash
git checkout main
git pull
```

---

##  2. Chuyển sang branch của bạn

```bash
git checkout feature/tennguoilam
```

Ví dụ:

```bash
git checkout feature/player-movement
```

---

##  3. Cập nhật branch của bạn

```bash
git merge main
```

---

##  4. Mở Unity → làm việc

* Code
* Chỉnh prefab / scene
* Thêm asset

 Không sửa file người khác đang làm

---

##  5. Lưu thay đổi

```bash
git add .
git commit -m "mô tả thay đổi"
```

---

##  6. Đẩy lên GitHub

```bash
git push
```

---

#  QUY TẮC BRANCH

##  Mỗi người tự tạo branch

```bash
git checkout -b feature/ten-task
git push -u origin feature/ten-task
```

---

##  Đặt tên branch(đặt theo tên mình nha)

```
feature/hoang
feature/quan
feature/binh
...
```

---

##  KHÔNG làm trực tiếp trên main

---

#  MERGE CODE

## Cách chuẩn (khuyên dùng)

1. Lên GitHub
2. Tạo Pull Request
3. Review code
4. Nhấn Merge

---

#  TRÁNH CONFLICT UNITY

##  KHÔNG làm

* 2 người sửa cùng 1 file `.unity`
* 2 người sửa cùng prefab

---

##  NÊN làm

* Mỗi người làm 1 prefab riêng
* Chia scene nếu cần
* Thông báo khi sửa scene chung

---

#  CÀI ĐẶT UNITY (BẮT BUỘC)

Vào:

```
Edit → Project Settings → Editor
```

* Version Control: **Visible Meta Files**
* Asset Serialization: **Force Text**

---

#  XỬ LÝ CONFLICT

Nếu bị conflict:

```bash
git checkout --theirs file
```

hoặc:

```bash
git checkout --ours file
```

Sau đó:

```bash
git add .
git commit -m "resolve conflict"
```

---

#  NGUYÊN TẮC VÀNG

* Luôn `git pull` trước khi làm
* Không làm trên `main`
* Commit thường xuyên
* Không đụng file người khác

---

#  TÓM TẮT NHANH

## Khi bắt đầu:

```bash
git checkout main
git pull
git checkout feature/ten-ban
git merge main
```

## Khi làm xong:

```bash
git add .
git commit -m "..."
git push
```

---

#  GHI NHỚ

 Mỗi người:

* Tự tạo branch
* Làm việc riêng
* Merge qua Pull Request

---

**Làm đúng quy trình này → project ổn định, không lỗi, không mất dữ liệu **
