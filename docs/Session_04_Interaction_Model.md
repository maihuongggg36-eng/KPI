# TÀI LIỆU MÔ HÌNH TƯƠNG TÁC (INTERACTION MODEL)
## HỆ THỐNG QUẢN LÝ VÀ ĐÁNH GIÁ KPI CHO DOANH NGHIỆP VÀ CÁN BỘ CÔNG CHỨC
**Mã tài liệu:** IM-KPI-V1.0  
**Giai đoạn:** Phiên 4 / 10 - Mô hình hóa Tương tác (Interaction Modeling)  
**Tác giả:** Chuyên gia Phân tích Hệ thống (Senior System Analyst)  
**Tài liệu tham chiếu:** [SRS Phiên 1 (SRS-KPI-V1.0)](file:///d:/KPI/docs/Session_01_SRS_Requirements.md) | [Use Case Model Phiên 2 (UCM-KPI-V1.0)](file:///d:/KPI/docs/Session_02_UseCase_Model.md) | [Class Model Phiên 3 (ACM-KPI-V1.0)](file:///d:/KPI/docs/Session_03_Class_Analysis_Model.md)  

---

## MỤC LỤC
1. [Giới thiệu Mô hình Tương tác & Nguyên tắc Thiết kế](#1-giới-thiệu-mô-hình-tương-tác--nguyên-tắc-thiết-kế)
2. [Hệ thống Biểu đồ Tuần tự (Sequence Diagrams)](#2-hệ-thống-biểu-đồ-tuần-tự-sequence-diagrams)
   - SD-01: Quy trình Đăng nhập, Kiểm soát An toàn & Phân quyền JWT
   - SD-02: Quy trình Lập kế hoạch & Giao KPI Cá nhân
   - SD-03: Quy trình Tự đánh giá KPI & Đính kèm Minh chứng số
   - SD-04: Quy trình Quản lý Thẩm định & Chấm điểm Nhân viên
   - SD-05: Quy trình Đánh giá Phân loại Cán bộ Công chức theo Nghị định 90
   - SD-06: Quy trình Tổng hợp Dashboard & Xuất Báo cáo Đa định dạng
   - SD-07: Quy trình Quản lý Danh mục KPI & Ràng buộc Toàn vẹn
   - SD-08: Quy trình Quét Tác vụ ngầm (Scheduler) & Gửi Cảnh báo Deadline
3. [Hệ thống Biểu đồ Giao tiếp (Communication Diagrams)](#3-hệ-thống-biểu-đồ-giao-tiếp-communication-diagrams)
   - CD-01: Quy trình Giao KPI & Cam kết Mục tiêu
   - CD-02: Quy trình Đánh giá & Thẩm định Kết quả KPI
   - CD-03: Quy trình Phê duyệt Đánh giá Đa cấp
4. [Bảng Tổng hợp Ánh xạ Thông điệp & Phương thức Lớp](#4-bảng-tổng-hợp-ánh-xạ-thông-điệp--phương-thức-lớp)

---

## 1. GIỚI THIỆU MÔ HÌNH TƯƠNG TÁC & NGUYÊN TẮC THIẾT KẾ

Mô hình tương tác thể hiện hành vi động (Dynamic Behavior) của hệ thống bằng cách mô tả dòng trao đổi thông điệp (Messages) giữa các đối tượng thuộc 3 tầng kiến trúc BCE:
1. **Actor** $\to$ Tương tác trực tiếp với **Boundary Object** (Giao diện Web/Form/API).
2. **Boundary Object** $\to$ Chuyển tiếp sự kiện và tham số đầu vào đến **Control Object** (Bộ điều khiển nghiệp vụ).
3. **Control Object** $\to$ Thực thi logic nghiệp vụ, gọi các dịch vụ kiểm tra (Validators), truy xuất và thay đổi trạng thái của các **Entity Objects** (Thực thể CSDL).
4. Các luồng xử lý đều tích hợp đầy đủ các cấu trúc rẽ nhánh (`alt`), lặp (`loop`), tùy chọn (`opt`) và xử lý ngoại lệ theo đúng 20 Ràng buộc nghiệp vụ (**BR-01 $\to$ BR-20**).

---

## 2. HỆ THỐNG BIỂU ĐỒ TUẦN TỰ (SEQUENCE DIAGRAMS)

---

### SD-01: QUY TRÌNH ĐĂNG NHẬP, KIỂM SOÁT AN TOÀN & PHÂN QUYỀN JWT
- **Use Case ánh xạ:** `UC-01: Đăng nhập & Xác thực hệ thống`
- **Mục tiêu:** Xác thực danh tính người dùng, kiểm soát chống brute-force (khóa tài khoản khi sai quá 5 lần), hỗ trợ 2FA OTP và cấp phát JWT Token.

```mermaid
sequenceDiagram
    autonumber
    actor User as Người dùng (User)
    participant UI as <<boundary>><br/>LoginFormView
    participant Ctrl as <<control>><br/>AuthController
    participant UserEnt as <<entity>><br/>User
    participant Audit as <<entity>><br/>AuditLog
    participant Mail as <<control>><br/>EmailService

    User->>UI: 1. Nhập username, password & nhấn "Đăng nhập"
    activate UI
    UI->>Ctrl: 2. postLogin(username, password)
    activate Ctrl

    Ctrl->>UserEnt: 3. findByUsername(username)
    activate UserEnt
    UserEnt-->>Ctrl: 4. return user
    deactivate UserEnt

    alt Tài khoản không tồn tại
        Ctrl-->>UI: 5a. Báo lỗi: "Tài khoản hoặc mật khẩu không chính xác"
    else Tài khoản đang bị khóa (status == "Locked")
        Ctrl-->>UI: 5b. Báo lỗi: "Tài khoản bị khóa đến " + lockedUntil
    else Tài khoản hợp lệ
        Ctrl->>UserEnt: 6. authenticate(password)
        activate UserEnt
        UserEnt-->>Ctrl: 7. return isPasswordMatch
        deactivate UserEnt

        alt Mật khẩu sai (isPasswordMatch == false)
            Ctrl->>UserEnt: 8a. increaseFailedAttempts()
            activate UserEnt
            opt failedAttempts >= 5
                UserEnt->>UserEnt: 8a.1. lockAccount(30 phút)
                Ctrl->>Mail: 8a.2. sendSecurityAlertEmail(user.email)
            end
            deactivate UserEnt
            Ctrl->>Audit: 8a.3. recordLog("LOGIN_FAILED", username)
            Ctrl-->>UI: 8a.4. Báo lỗi: "Mật khẩu không đúng. Còn lại X lần thử"
        else Mật khẩu đúng (isPasswordMatch == true)
            Ctrl->>UserEnt: 8b. resetFailedAttempts()
            
            opt Bật xác thực 2 bước (user.is2faEnabled == true)
                Ctrl->>Mail: 8b.1. generateAndSendOTP(user.email)
                Ctrl-->>UI: 8b.2. Yêu cầu hiển thị modal nhập mã OTP 6 số
                User->>UI: 8b.3. Nhập mã OTP
                UI->>Ctrl: 8b.4. verifyOTP(code)
                Ctrl->>Ctrl: 8b.5. validateOTP(code)
            end

            Ctrl->>Ctrl: 9. generateJwtToken(user, roles, permissions, dataScope)
            Ctrl->>Audit: 10. recordLog("LOGIN_SUCCESS", user.id)
            Ctrl-->>UI: 11. return AuthResponse(accessToken, refreshToken, userProfile)
            deactivate Ctrl
            UI-->>User: 12. Chuyển hướng đến Dashboard theo vai trò
            deactivate UI
        end
    end
```

#### Bảng mô tả chi tiết các bước xử lý (SD-01):
1. **Bước 1-4:** Người dùng nhập thông tin xác thực. `LoginFormView` gửi yêu cầu đến `AuthController`. Controller truy vấn CSDL để lấy thông tin tài khoản.
2. **Bước 5a-5b:** Kiểm tra tài khoản tồn tại và trạng thái kích hoạt. Nếu tài khoản đang bị khóa do vi phạm brute-force trước đó, trả thông báo chặn tức thì.
3. **Bước 6-8a:** So khớp mật khẩu đã băm (Argon2id). Nếu sai mật khẩu: tăng biến đếm `failedAttempts`. Khi chạm mốc 5 lần, tự động kích hoạt khóa tài khoản 30 phút, gửi email cảnh báo bảo mật và ghi vết `AuditLog`.
4. **Bước 8b:** Mật khẩu đúng: Reset số lần thử sai. Nếu tài khoản có cấu hình bảo mật 2FA, hệ thống sinh mã OTP ngẫu nhiên gửi qua email/authenticator và chờ người dùng xác thực mã.
5. **Bước 9-12:** Đóng gói thông tin người dùng, quyền hạn (Roles/Permissions) và phạm vi dữ liệu (`DataScope`) vào JWT Claims. Cấp phát cặp Access/Refresh Token và chuyển hướng người dùng vào Dashboard tương ứng.

---

### SD-02: QUY TRÌNH LẬP KẾ HOẠCH & GIAO KPI CÁ NHÂN
- **Use Case ánh xạ:** `UC-05: Giao KPI & Thiết lập trọng số cá nhân`
- **Ràng buộc nghiệp vụ áp dụng:** **BR-01** ($\sum \text{Weight} = 100\%$), **BR-02** ($5\% \le \text{Weight}_i \le 50\%$), **BR-13** (Kiểm soát kiêm nhiệm).

```mermaid
sequenceDiagram
    autonumber
    actor Mgr as Quản lý phòng ban (Manager)
    participant UI as <<boundary>><br/>KPIAssignmentFormView
    participant Ctrl as <<control>><br/>KPIAssignmentController
    participant Val as <<control>><br/>WeightValidatorService
    participant Tpl as <<entity>><br/>KPITemplate
    participant Assign as <<entity>><br/>KPIAssignment
    participant Detail as <<entity>><br/>KPIAssignmentDetail
    participant Notif as <<control>><br/>NotificationDispatcher

    Mgr->>UI: 1. Chọn nhân viên & nhấn "Giao KPI"
    activate UI
    UI->>Ctrl: 2. getInitialData(employeeId, periodId)
    activate Ctrl
    Ctrl->>Tpl: 3. findByPosition(employee.positionId)
    activate Tpl
    Tpl-->>Ctrl: 4. return templateDetails
    deactivate Tpl
    Ctrl-->>UI: 5. Hiển thị form kèm danh sách KPI mẫu gợi ý
    deactivate Ctrl

    Mgr->>UI: 6. Tinh chỉnh Target & Trọng số cho từng chỉ tiêu
    Mgr->>UI: 7. Nhấn "Gửi cam kết chỉ tiêu"
    UI->>Ctrl: 8. submitKPIAssignment(dto)
    activate Ctrl

    Ctrl->>Val: 9. validateWeights(dto.items)
    activate Val
    Val->>Val: 9.1. Kiểm tra từng item: 5% <= weight <= 50% (BR-02)
    Val->>Val: 9.2. Tính tổng: sumWeight = sum(item.weight)
    
    alt sumWeight != 100% (Vi phạm BR-01)
        Val-->>Ctrl: 10a. return Invalid("Tổng trọng số phải bằng 100%. Hiện tại: " + sumWeight + "%")
        deactivate Val
        Ctrl-->>UI: 10a.1. Hiển thị thông báo lỗi màu đỏ, chặn lưu
    else Hợp lệ (sumWeight == 100%)
        Val-->>Ctrl: 10b. return Valid
        
        Ctrl->>Assign: 11. create(periodId, employeeId, totalWeight=100, status="Pending_Staff_Confirm")
        activate Assign
        Assign-->>Ctrl: 12. return assignmentId
        deactivate Assign

        loop Với mỗi chỉ tiêu KPI trong danh sách
            Ctrl->>Detail: 13. createDetail(assignmentId, kpiItemId, targetVal, weight, formula)
        end

        Ctrl->>Notif: 14. notifyKPIAssigned(employeeId, assignmentId)
        activate Notif
        Notif--)Notif: 14.1. Tạo thông báo In-App & Đẩy email hàng đợi
        deactivate Notif

        Ctrl-->>UI: 15. return Success("Đã gửi bản giao KPI thành công")
        deactivate Ctrl
        UI-->>Mgr: 16. Thông báo thành công & cập nhật trạng thái trên bảng danh sách
        deactivate UI
    end
```

#### Bảng mô tả chi tiết các bước xử lý (SD-02):
1. **Bước 1-5:** Quản lý mở form giao KPI. Hệ thống tự động truy xuất `KPITemplate` gắn liền với vị trí chức danh của nhân viên để điền trước danh mục chỉ tiêu chuẩn.
2. **Bước 6-8:** Quản lý điều chỉnh các giá trị mục tiêu (Target) và tỷ lệ phần trăm trọng số (% Weight) cho phù hợp với kế hoạch kỳ đánh giá.
3. **Bước 9-10a:** `WeightValidatorService` thực hiện 2 tầng kiểm tra nghiêm ngặt: Mỗi chỉ tiêu đơn lẻ phải nằm trong khoảng $[5\%, 50\%]$ (**BR-02**) và tổng trọng số toàn bộ bản giao phải bằng chính xác $100\%$ (**BR-01**). Nếu sai lệch, hệ thống hiển thị thông báo chặn cưỡng bức.
4. **Bước 11-13:** Khởi tạo thực thể `KPIAssignment` ở trạng thái `Pending_Staff_Confirm` và tạo hàng loạt các thực thể con `KPIAssignmentDetail` liên kết hợp thành (Composition).
5. **Bước 14-16:** Kích hoạt dịch vụ thông báo đa kênh (In-app và Email) gửi lời mời cam kết mục tiêu đến nhân viên.

---

### SD-03: QUY TRÌNH TỰ ĐÁNH GIÁ KPI & ĐÍNH KÈM MINH CHỨNG
- **Use Case ánh xạ:** `UC-06: Nhân viên tự đánh giá KPI cá nhân`
- **Ràng buộc nghiệp vụ áp dụng:** **BR-09** (Bắt buộc minh chứng khi đạt $\ge 120\%$), **BR-10 & BR-11** (Công thức tính % hoàn thành), **BR-12** (Kiểm soát hạn chót).

```mermaid
sequenceDiagram
    autonumber
    actor Emp as Cán bộ / Nhân viên (Employee)
    participant UI as <<boundary>><br/>SelfAssessmentFormView
    participant Ctrl as <<control>><br/>SelfAssessmentController
    participant Assign as <<entity>><br/>KPIAssignment
    participant Detail as <<entity>><br/>KPIAssignmentDetail
    participant Eva as <<entity>><br/>Evaluation
    participant FileServ as <<control>><br/>FileStorageService

    Emp->>UI: 1. Mở phiếu tự đánh giá của kỳ hiện tại
    activate UI
    UI->>Ctrl: 2. getAssessmentForm(assignmentId)
    activate Ctrl
    Ctrl->>Assign: 3. loadAssignmentWithDetails(assignmentId)
    activate Assign
    Assign-->>Ctrl: 4. return assignmentData
    deactivate Assign
    Ctrl-->>UI: 5. Hiển thị danh sách chỉ tiêu, Target và Trọng số
    deactivate Ctrl

    loop Với từng chỉ tiêu KPI
        Emp->>UI: 6. Nhập ActualValue (Kết quả thực tế)
        UI->>Ctrl: 7. calculatePreviewScore(detailId, actualVal)
        activate Ctrl
        Ctrl->>Detail: 8. calculateAchievement(actualVal)
        activate Detail
        Detail-->>Ctrl: 9. return completionRate, score
        deactivate Detail
        Ctrl-->>UI: 10. Trả về % hoàn thành tạm tính & điểm thành phần
        deactivate Ctrl

        opt Kết quả đạt >= 120% (Kích hoạt quy tắc BR-09)
            UI-->>Emp: 11. Bật cờ đỏ: "Bắt buộc đính kèm tệp minh chứng"
            Emp->>UI: 12. Chọn tệp minh chứng (PDF/Excel) & upload
            UI->>FileServ: 13. uploadEvidence(file)
            activate FileServ
            FileServ-->>UI: 14. return fileUrl, fileMetadata
            deactivate FileServ
        end
    end

    Emp->>UI: 15. Nhập bản tự nhận xét cá nhân & nhấn "Nộp tự đánh giá"
    UI->>Ctrl: 16. submitSelfAssessment(assessmentDto)
    activate Ctrl

    Ctrl->>Ctrl: 17. validateSubmission(assessmentDto)
    alt Có chỉ tiêu >= 120% nhưng thiếu tệp minh chứng (BR-09)
        Ctrl-->>UI: 18a. Báo lỗi: "Chỉ tiêu X đạt >= 120% bắt buộc phải có tệp minh chứng đính kèm"
    else Hợp lệ
        Ctrl->>Eva: 19. saveSelfScores(selfTotalScore, details, status="Self_Submitted")
        activate Eva
        Eva-->>Ctrl: 20. return evaluationId
        deactivate Eva

        Ctrl->>Assign: 21. updateStatus("In_Evaluation")
        Ctrl-->>UI: 22. return Success("Đã nộp phiếu tự đánh giá thành công")
        deactivate Ctrl
        UI-->>Emp: 23. Hiển thị thông báo hoàn thành & khóa chế độ chỉnh sửa của nhân viên
        deactivate UI
    end
```

#### Bảng mô tả chi tiết các bước xử lý (SD-03):
1. **Bước 1-5:** Nhân viên mở phiếu tự đánh giá. Giao diện hiển thị chi tiết các chỉ tiêu cam kết đầu kỳ.
2. **Bước 6-10:** Khi nhân viên nhập số liệu thực hiện (`ActualValue`), Controller tự động tính toán tỷ lệ % hoàn thành theo đúng công thức:
   - Chỉ số càng cao càng tốt: $\% = \min(\text{Thực tế} / \text{Kế hoạch} \times 100\%, 150\%)$ (**BR-10**).
   - Chỉ số càng thấp càng tốt: $\% = \max((2 - \text{Thực tế} / \text{Kế hoạch}) \times 100\%, 0\%)$ (**BR-11**).
3. **Bước 11-14:** Nếu tỷ lệ hoàn thành $\ge 120\%$, giao diện kích hoạt bắt buộc người dùng tải lên tệp minh chứng số thông qua `FileStorageService` (**BR-09**).
4. **Bước 15-18a:** Khi nhấn nộp, hệ thống kiểm tra toàn vẹn minh chứng. Nếu thiếu minh chứng cho các chỉ tiêu vượt mức, hệ thống từ chối lưu.
5. **Bước 19-23:** Lưu kết quả tự chấm vào thực thể `Evaluation`, chuyển trạng thái sang `Self_Submitted` và khóa form tự đánh giá.

---

### SD-04: QUY TRÌNH QUẢN LÝ THẨM ĐỊNH & CHẤM ĐIỂM
- **Use Case ánh xạ:** `UC-07: Quản lý đánh giá KPI nhân viên`
- **Ràng buộc nghiệp vụ áp dụng:** **BR-04** (Thứ tự đánh giá), **BR-05** (Bắt buộc giải trình điều chỉnh lệch $> 10\%$).

```mermaid
sequenceDiagram
    autonumber
    actor Mgr as Quản lý phòng ban (Manager)
    participant UI as <<boundary>><br/>ManagerReviewFormView
    participant Ctrl as <<control>><br/>ManagerReviewController
    participant Eva as <<entity>><br/>Evaluation
    participant Detail as <<entity>><br/>EvaluationDetail
    participant WF as <<control>><br/>EvaluationWorkflowEngine

    Mgr->>UI: 1. Mở danh sách hồ sơ cần chấm điểm
    activate UI
    UI->>Ctrl: 2. getReviewDetail(evaluationId)
    activate Ctrl
    Ctrl->>Eva: 3. loadEvaluationWithEvidences(evaluationId)
    activate Eva
    Eva-->>Ctrl: 4. return evaluationDetailData
    deactivate Eva
    Ctrl-->>UI: 5. Hiển thị giao diện 2 cột: Điểm tự chấm vs Cột Quản lý chấm
    deactivate Ctrl

    loop Với từng chỉ tiêu KPI
        Mgr->>UI: 6. Xem minh chứng & Nhập điểm Quản lý (managerScore)
        UI->>UI: 7. Tính độ lệch điểm: scoreGap = |selfScore - managerScore|
        
        opt scoreGap > 10% (Kích hoạt quy tắc BR-05)
            UI-->>Mgr: 8. Hiển thị ô bắt buộc: "Nhập lý do điều chỉnh điểm (>= 50 ký tự)"
            Mgr->>UI: 9. Nhập nội dung giải trình lý do chênh lệch điểm
        end
    end

    Mgr->>UI: 10. Nhập nhận xét tổng kết & Nhấn "Hoàn thành thẩm định"
    UI->>Ctrl: 11. submitManagerReview(reviewDto)
    activate Ctrl

    Ctrl->>Ctrl: 12. validateScoreGapReasons(reviewDto)
    alt Tồn tại chỉ tiêu lệch > 10% nhưng thiếu lý do giải trình (BR-05)
        Ctrl-->>UI: 13a. Báo lỗi: "Vui lòng nhập lý do giải trình cho các chỉ tiêu lệch trên 10%"
    else Hợp lệ
        Ctrl->>Detail: 14. updateManagerScoresAndReasons(reviewDto.items)
        Ctrl->>Eva: 15. calculateManagerTotalScore()
        activate Eva
        Eva->>Eva: 15.1. managerTotalScore = sum(item.managerScore * item.weight)
        Eva-->>Ctrl: 15.2. return totalScore
        deactivate Eva

        Ctrl->>WF: 16. advanceWorkflow(evaluationId, "Manager_Reviewed")
        activate WF
        WF->>WF: 16.1. Ghi nhận lịch sử vào WorkflowHistory
        WF-->>Ctrl: 16.2. return nextStep ("Pending_Director_Approval")
        deactivate WF

        Ctrl-->>UI: 17. return Success("Đã lưu kết quả thẩm định và chuyển cấp phê duyệt")
        deactivate Ctrl
        UI-->>Mgr: 18. Thông báo thành công & điều hướng về danh sách quản lý
        deactivate UI
    end
```

#### Bảng mô tả chi tiết các bước xử lý (SD-04):
1. **Bước 1-5:** Quản lý truy cập hồ sơ. Hệ thống tải dữ liệu phiếu đánh giá và mở giao diện so sánh đối chiếu song song: Cột điểm tự chấm kèm ý kiến giải trình của nhân viên và Cột nhập điểm chính thức của Quản lý.
2. **Bước 6-9:** Quản lý xem xét minh chứng và nhập điểm thẩm định. Giao diện tự động tính độ lệch $|selfScore - managerScore|$. Nếu độ lệch vượt quá $10\%$, hệ thống kích hoạt trường bắt buộc: *Lý do điều chỉnh điểm* ($\ge 50$ ký tự) theo quy định tại **BR-05**.
3. **Bước 10-13a:** Khi nhấn nộp, Controller kiểm tra validation. Nếu phát hiện thiếu lý do giải trình ở bất kỳ chỉ tiêu nào có độ lệch $> 10\%$, hệ thống chặn lại và focus vào ô tương ứng.
4. **Bước 14-18:** Cập nhật điểm thẩm định, tính điểm tổng hợp theo trọng số thang 100 (**BR-20**) và gọi `EvaluationWorkflowEngine` để đẩy hồ sơ lên cấp phê duyệt tiếp theo.

---

### SD-05: QUY TRÌNH ĐÁNH GIÁ PHÂN LOẠI CÁN BỘ CÔNG CHỨC THEO NGHỊ ĐỊNH 90
- **Use Case ánh xạ:** `UC-10: Đánh giá phân loại cán bộ công chức theo Nghị định 90/2020/NĐ-CP`
- **Ràng buộc nghiệp vụ áp dụng:** **BR-06** (Chặn khống chế Xuất sắc $\le 20\%$), **BR-07** (Điểm liệt do kỷ luật), **BR-08** (Bắt buộc sáng kiến/đề tài).

```mermaid
sequenceDiagram
    autonumber
    actor Dir as Thủ trưởng cơ quan (Director)
    participant UI as <<boundary>><br/>CivilServantEvaluationView
    participant Ctrl as <<control>><br/>CivilServantClassificationService
    participant PubEnt as <<entity>><br/>CivilServantClassification
    participant Eva as <<entity>><br/>Evaluation
    participant RepGen as <<control>><br/>ReportGeneratorService

    Dir->>UI: 1. Mở danh sách xếp loại công chức toàn cơ quan
    activate UI
    UI->>Ctrl: 2. getClassificationSummary(periodId, orgId)
    activate Ctrl
    Ctrl->>PubEnt: 3. loadAllOfficialRanks(periodId)
    activate PubEnt
    PubEnt-->>Ctrl: 4. return officialList
    deactivate PubEnt
    Ctrl-->>UI: 5. Hiển thị danh sách cán bộ, điểm KPI, điểm chính trị & đề xuất xếp loại
    deactivate Ctrl

    loop Với từng cán bộ công chức
        Dir->>UI: 6. Chọn mức phân loại chất lượng
        
        alt Cán bộ bị kỷ luật Đảng / Hành chính trong năm (hasDisciplinaryAction == true)
            UI-->>Dir: 7a. Cảnh báo BR-07: Tự động khóa mức Xuất sắc/Tốt/Hoàn thành. Ép xếp "Không hoàn thành nhiệm vụ"
        else Chọn mức "Hoàn thành xuất sắc nhiệm vụ"
            opt Không có đề tài / sáng kiến công nhận (BR-08)
                UI-->>Dir: 7b. Cảnh báo BR-08: Cán bộ thiếu điều kiện Sáng kiến cải tiến công vụ
            end
        end
    end

    Dir->>UI: 8. Nhấn nút "Phê duyệt phân loại toàn cơ quan"
    UI->>Ctrl: 9. finalizeClassifications(periodId, classificationList)
    activate Ctrl

    Ctrl->>Ctrl: 10. validateDecree90Rules(classificationList)
    Ctrl->>Ctrl: 10.1. Đếm countGood = Tổng cán bộ xếp loại "Hoàn thành tốt nhiệm vụ"
    Ctrl->>Ctrl: 10.2. Đếm countExc = Tổng cán bộ xếp loại "Hoàn thành xuất sắc nhiệm vụ"
    Ctrl->>Ctrl: 10.3. maxAllowedExc = floor(0.20 * countGood)

    alt countExc > maxAllowedExc (Vi phạm nghiêm trọng BR-06 & Nghị định 90)
        Ctrl-->>UI: 11a. Báo lỗi nghiêm trọng: "Tỷ lệ Xuất sắc (" + countExc + "/" + countGood + ") vượt quá 20%. Không thể phê duyệt!"
    else Tỷ lệ hợp lệ (countExc <= maxAllowedExc)
        Ctrl->>PubEnt: 12. saveFinalizedRanks(classificationList)
        Ctrl->>Eva: 13. updateFinalStatus("Final_Approved_Locked")
        
        Ctrl->>RepGen: 14. generateDecreeForms(periodId, "Form_01_02")
        activate RepGen
        RepGen-->>Ctrl: 15. return pdfFilesList
        deactivate RepGen

        Ctrl-->>UI: 16. return Success("Phê duyệt thành công. Đã sinh Phiếu Mẫu 01/02 chuẩn Nghị định")
        deactivate Ctrl
        UI-->>Dir: 17. Hiển thị thông báo thành công & nút tải trọn bộ hồ sơ cán bộ
        deactivate UI
    end
```

#### Bảng mô tả chi tiết các bước xử lý (SD-05):
1. **Bước 1-5:** Thủ trưởng cơ quan mở màn hình đánh giá phân loại cán bộ công chức. Hệ thống tổng hợp đầy đủ số liệu 2 phần: Điểm KPI thực hiện nhiệm vụ và Điểm phẩm chất chính trị tư tưởng, đạo đức, tác phong, kỷ luật.
2. **Bước 6-7a:** Nếu hồ sơ có ghi nhận kỷ luật Đảng hoặc kỷ luật hành chính trong năm, áp dụng **BR-07**: Tự động khóa các mức hoàn thành và ép chuyển sang *Không hoàn thành nhiệm vụ*.
3. **Bước 7b:** Nếu chọn mức *Hoàn thành xuất sắc*, hệ thống đối soát điều kiện có sáng kiến cải tiến hoặc đề tài khoa học theo **BR-08**.
4. **Bước 8-11a:** Khi Thủ trưởng bấm duyệt, `CivilServantClassificationService` tiến hành kiểm tra công thức chặn trần: $N_{\text{Xuất sắc}} \le 0.20 \times N_{\text{Hoàn thành tốt}}$ (**BR-06**). Nếu vi phạm, hệ thống khóa cứng nút phê duyệt và hiển thị số lượng cụ thể cần điều chỉnh giảm.
5. **Bước 12-17:** Khi tỷ lệ chuẩn hóa, hệ thống đóng băng toàn bộ dữ liệu (**BR-15**) và tự động trích xuất các biểu mẫu Mẫu 01/02 đính kèm hồ sơ cán bộ.

---

### SD-06: QUY TRÌNH TỔNG HỢP DASHBOARD & XUẤT BÁO CÁO
- **Use Case ánh xạ:** `UC-09: Xem Dashboard tổng quan & Xuất báo cáo`
- **Mục tiêu:** Tính toán số liệu phân tích thời gian thực (Real-time Analytics) và xuất báo cáo Excel/PDF.

```mermaid
sequenceDiagram
    autonumber
    actor User as Người dùng (Director / HR Admin)
    participant UI as <<boundary>><br/>ExecutiveDashboardView
    participant Ctrl as <<control>><br/>DashboardAnalyticsController
    participant RepServ as <<control>><br/>ReportGeneratorService
    participant Eva as <<entity>><br/>Evaluation
    participant Cache as <<control>><br/>RedisCacheService

    User->>UI: 1. Truy cập trang Dashboard Lãnh đạo
    activate UI
    UI->>Ctrl: 2. getExecutiveAnalytics(periodId, orgId)
    activate Ctrl

    Ctrl->>Cache: 3. getCachedDashboard(periodId, orgId)
    activate Cache
    Cache-->>Ctrl: 4. return cachedData (hoặc null nếu cache miss)
    deactivate Cache

    opt Cache Miss (Chưa có bộ nhớ đệm)
        Ctrl->>Eva: 5. queryAggregatedData(periodId, orgId)
        activate Eva
        Eva-->>Ctrl: 6. return rawRecords
        deactivate Eva
        Ctrl->>Ctrl: 7. calculateGaussBellDistribution(rawRecords)
        Ctrl->>Ctrl: 8. calculateDepartmentRankings(rawRecords)
        Ctrl->>Cache: 9. setCache(dashboardData, ttl=15 phút)
    end

    Ctrl-->>UI: 10. Trả về Dashboard DTO (Biểu đồ hình chuông, Top/Bottom 5, Tỷ lệ nộp)
    deactivate Ctrl
    UI-->>User: 11. Render trực quan biểu đồ tương tác (< 1.5 giây)

    User->>UI: 12. Chọn "Xuất báo cáo tổng hợp", chọn định dạng "Excel"
    UI->>RepServ: 13. exportReport(periodId, orgId, format="XLSX")
    activate RepServ
    RepServ->>RepServ: 14. buildExcelWorkbookWithFormulas()
    RepServ-->>UI: 15. return fileDownloadUrl
    deactivate RepServ
    UI-->>User: 16. Tự động tải file Excel về máy trạm
    deactivate UI
```

#### Bảng mô tả chi tiết các bước xử lý (SD-06):
1. **Bước 1-4:** Người dùng mở Dashboard. Controller ưu tiên kiểm tra dữ liệu tính toán sẵn trong bộ nhớ đệm tốc độ cao (`RedisCacheService`).
2. **Bước 5-9:** Nếu cache hết hạn, Controller truy vấn dữ liệu từ bảng `evaluations`, tính toán đường cong phân phối chuẩn (Gaussian Bell Curve) và bảng xếp hạng thi đua, sau đó tái nạp vào Cache với thời gian sống 15 phút.
3. **Bước 10-11:** Hiển thị trực quan dữ liệu lên màn hình trong thời gian đáp ứng $< 1.5$ giây (đạt chuẩn **NFR-PERF-02**).
4. **Bước 12-16:** Khi người dùng yêu cầu kết xuất file, `ReportGeneratorService` tạo tài liệu Excel đầy đủ công thức và tải trực tiếp về máy tính người dùng.

---

### SD-07: QUY TRÌNH QUẢN LÝ DANH MỤC KPI & RÀNG BUỘC TOÀN VẸN
- **Use Case ánh xạ:** `UC-03: Quản lý danh mục KPI dùng chung`
- **Mục tiêu:** Thực hiện thêm mới/chỉnh sửa chỉ số KPI, kiểm tra tính duy nhất của Mã chỉ số và ngăn chặn xóa các danh mục đang có liên kết dữ liệu.

```mermaid
sequenceDiagram
    autonumber
    actor HR as Chuyên viên Nhân sự (HR Admin)
    participant UI as <<boundary>><br/>CatalogFormModal
    participant Ctrl as <<control>><br/>CatalogManagementController
    participant Master as <<entity>><br/>KPIMasterItem
    participant Detail as <<entity>><br/>KPIAssignmentDetail

    HR->>UI: 1. Nhập thông tin KPI mới & nhấn "Lưu chỉ số"
    activate UI
    UI->>Ctrl: 2. createKPIItem(itemDto)
    activate Ctrl

    Ctrl->>Master: 3. existsByCode(itemDto.code)
    activate Master
    Master-->>Ctrl: 4. return isCodeExisted
    deactivate Master

    alt Trùng mã chỉ số (isCodeExisted == true)
        Ctrl-->>UI: 5a. Báo lỗi: "Mã chỉ số KPI đã tồn tại trên hệ thống"
    else Mã hợp lệ
        Ctrl->>Master: 6. save(itemDto)
        activate Master
        Master-->>Ctrl: 7. return savedItem
        deactivate Master
        Ctrl-->>UI: 8. return Success("Đã thêm mới chỉ số KPI thành công")
    end
    deactivate Ctrl

    HR->>UI: 9. Chọn xóa một chỉ số KPI cũ
    UI->>Ctrl: 10. deleteKPIItem(itemId)
    activate Ctrl
    Ctrl->>Detail: 11. countUsageInAssignments(itemId)
    activate Detail
    Detail-->>Ctrl: 12. return usageCount
    deactivate Detail

    alt Đang được sử dụng trong bản giao KPI (usageCount > 0)
        Ctrl-->>UI: 13a. Báo lỗi: "Không thể xóa chỉ số đang được sử dụng trong kỳ đánh giá. Vui lòng chuyển trạng thái sang Không hoạt động"
    else Chưa từng sử dụng (usageCount == 0)
        Ctrl->>Master: 14. delete(itemId)
        Ctrl-->>UI: 15. return Success("Đã xóa chỉ số khỏi danh mục")
    end
    deactivate Ctrl
    deactivate UI
```

---

### SD-08: QUY TRÌNH QUÉT TÁC VỤ NGẦM & GỬI CẢNH BÁO DEADLINE
- **Use Case ánh xạ:** `UC-34: Tự động nhắc việc & Deadline`
- **Mục tiêu:** Bộ quét nền (Cron Scheduler) tự động rà soát các mốc hạn chót, phát hiện nhân sự trễ hạn, gửi cảnh báo In-App và đẩy Email hàng đợi.

```mermaid
sequenceDiagram
    autonumber
    participant Cron as <<secondary>><br/>System Cron Engine
    participant Job as <<control>><br/>DeadlineReminderJob
    participant Period as <<entity>><br/>EvaluationPeriod
    participant Assign as <<entity>><br/>KPIAssignment
    participant Notif as <<entity>><br/>Notification
    participant Queue as <<entity>><br/>EmailQueue
    participant SMTP as <<external>><br/>SMTP Mail Server

    Note over Cron,Job: Kích hoạt định kỳ hàng ngày vào lúc 08:00 AM
    Cron->>Job: 1. executeDailyScan()
    activate Job

    Job->>Period: 2. findActivePeriodsWithUpcomingDeadlines()
    activate Period
    Period-->>Job: 3. return activePeriodsList
    deactivate Period

    loop Với mỗi kỳ đánh giá đang mở
        Job->>Assign: 4. findUnfinishedAssignments(period.id, thresholdDays=[3, 1, 0])
        activate Assign
        Assign-->>Job: 5. return pendingStaffList
        deactivate Assign

        loop Với mỗi nhân sự sắp đến hạn
            Job->>Notif: 6. createInAppNotification(staff.id, "Nhắc nhở: Còn X ngày hết hạn nộp tự đánh giá")
            Job->>Queue: 7. enqueueEmail(staff.email, template="DEADLINE_REMINDER", params)
        end
    end

    Job->>Queue: 8. fetchPendingEmails(batchSize=50)
    activate Queue
    Queue-->>Job: 9. return pendingEmailsList
    deactivate Queue

    loop Với mỗi email trong hàng đợi
        Job->>SMTP: 10. sendMail(to, subject, htmlBody)
        activate SMTP
        SMTP-->>Job: 11. return DeliveryStatus
        deactivate SMTP
        
        alt Gửi thành công
            Job->>Queue: 12a. markSent(email.id)
        else Gửi thất bại
            Job->>Queue: 12b. increaseRetryCountOrMarkFailed(email.id)
        end
    end
    deactivate Job
```

---

## 3. HỆ THỐNG BIỂU ĐỒ GIAO TIẾP (COMMUNICATION DIAGRAMS)

---

### CD-01: QUY TRÌNH GIAO KPI & CAM KẾT MỤC TIÊU

Biểu đồ giao tiếp tập trung vào kiến trúc cấu trúc liên kết và dòng thông điệp đánh số phân cấp giữa các đối tượng trong phân hệ Lập kế hoạch:

```mermaid
flowchart TD
    Mgr(("fa:fa-user Manager\n(Quản lý)"))
    UI["1: KPIAssignmentFormView\n<<boundary>>"]
    Ctrl["2: KPIAssignmentController\n<<control>>"]
    Val["3: WeightValidatorService\n<<control>>"]
    Assign["4: KPIAssignment\n<<entity>>"]
    Detail["5: KPIAssignmentDetail\n<<entity>>"]
    Notif["6: NotificationDispatcher\n<<control>>"]

    Mgr -->|1: submitKPIAssignment()| UI
    UI -->|1.1: processAssignment()| Ctrl
    Ctrl -->|1.2: validateWeights()| Val
    Val -.->|1.2.1: [BR-01, BR-02 OK]| Ctrl
    Ctrl -->|1.3: create()| Assign
    Ctrl -->|1.4: [loop] createDetail()| Detail
    Ctrl -->|1.5: notifyStaff()| Notif
    Notif -.->|1.5.1: sendAlert()| Mgr

    classDef actor fill:#e0f2fe,stroke:#0284c7,stroke-width:2px;
    classDef boundary fill:#fef3c7,stroke:#d97706,stroke-width:2px;
    classDef control fill:#dbeafe,stroke:#2563eb,stroke-width:2px;
    classDef entity fill:#dcfce7,stroke:#16a34a,stroke-width:2px;

    class Mgr actor;
    class UI boundary;
    class Ctrl,Val,Notif control;
    class Assign,Detail entity;
```

#### Dòng thông điệp (Messages Sequence):
- **1:** Quản lý thực hiện nhấn gửi bản giao trên giao diện `KPIAssignmentFormView`.
- **1.1:** Giao diện gọi hàm `processAssignment(dto)` trên `KPIAssignmentController`.
- **1.2:** Controller chuyển danh sách chỉ tiêu cho `WeightValidatorService` để kiểm tra: $\sum \text{Weight} = 100\%$ (**BR-01**) và $5\% \le \text{Weight}_i \le 50\%$ (**BR-02**).
- **1.2.1:** Validator xác nhận hợp lệ.
- **1.3:** Controller khởi tạo thực thể bản ghi chính `KPIAssignment` ở trạng thái chờ cam kết.
- **1.4:** Vòng lặp khởi tạo từng thực thể chi tiết `KPIAssignmentDetail`.
- **1.5:** Controller gọi `NotificationDispatcher` để sinh thông báo và gửi thư điện tử cho nhân viên.

---

### CD-02: QUY TRÌNH ĐÁNH GIÁ & THẨM ĐỊNH KẾT QUẢ KPI

```mermaid
flowchart TD
    Emp(("fa:fa-user Employee\n(Nhân viên)"))
    Mgr(("fa:fa-user Manager\n(Quản lý)"))
    UI_Self["1: SelfAssessmentFormView\n<<boundary>>"]
    UI_Mgr["2: ManagerReviewFormView\n<<boundary>>"]
    Ctrl_Self["3: SelfAssessmentController\n<<control>>"]
    Ctrl_Mgr["4: ManagerReviewController\n<<control>>"]
    Eva["5: Evaluation\n<<entity>>"]
    Detail["6: EvaluationDetail\n<<entity>>"]
    WF["7: EvaluationWorkflowEngine\n<<control>>"]

    Emp -->|1: submitSelfAssessment()| UI_Self
    UI_Self -->|1.1: processSelfAssessment()| Ctrl_Self
    Ctrl_Self -->|1.2: calculateAchievement()| Detail
    Ctrl_Self -->|1.3: saveSelfScores()| Eva

    Mgr -->|2: submitManagerReview()| UI_Mgr
    UI_Mgr -->|2.1: processManagerReview()| Ctrl_Mgr
    Ctrl_Mgr -->|2.2: [opt BR-05] validateGapReason()| Ctrl_Mgr
    Ctrl_Mgr -->|2.3: updateScores()| Detail
    Ctrl_Mgr -->|2.4: calculateTotal()| Eva
    Ctrl_Mgr -->|2.5: advanceStep()| WF

    classDef actor fill:#e0f2fe,stroke:#0284c7,stroke-width:2px;
    classDef boundary fill:#fef3c7,stroke:#d97706,stroke-width:2px;
    classDef control fill:#dbeafe,stroke:#2563eb,stroke-width:2px;
    classDef entity fill:#dcfce7,stroke:#16a34a,stroke-width:2px;

    class Emp,Mgr actor;
    class UI_Self,UI_Mgr boundary;
    class Ctrl_Self,Ctrl_Mgr,WF control;
    class Eva,Detail entity;
```

#### Dòng thông điệp (Messages Sequence):
- **1 $\to$ 1.3:** Nhân viên tự chấm điểm $\to$ Giao diện chuyển controller $\to$ Tính điểm thành phần và lưu vào `Evaluation` và `EvaluationDetail`.
- **2 $\to$ 2.5:** Quản lý mở hồ sơ chấm điểm $\to$ Controller kiểm tra độ lệch điểm $> 10\%$ để bắt buộc nhập lý do (**BR-05**) $\to$ Cập nhật điểm Quản lý $\to$ Tính tổng điểm cuối kỳ $\to$ Báo cho `EvaluationWorkflowEngine` chuyển hồ sơ lên cấp phê duyệt.

---

### CD-03: QUY TRÌNH PHÊ DUYỆT ĐÁNH GIÁ ĐA CẤP

```mermaid
flowchart TD
    Dir(("fa:fa-user Director\n(Lãnh đạo)"))
    UI["1: ApprovalWorkflowModal\n<<boundary>>"]
    Ctrl["2: CivilServantClassificationService\n<<control>>"]
    WF["3: EvaluationWorkflowEngine\n<<control>>"]
    Pub["4: CivilServantClassification\n<<entity>>"]
    Eva["5: Evaluation\n<<entity>>"]
    Rep["6: ReportGeneratorService\n<<control>>"]

    Dir -->|1: approveFinalRankings()| UI
    UI -->|1.1: executeApproval()| Ctrl
    Ctrl -->|1.2: [BR-06] validate20PercentCap()| Ctrl
    Ctrl -->|1.3: [BR-07] verifyDisciplinaryPenalty()| Pub
    Ctrl -->|1.4: lockFinalScores()| Eva
    Ctrl -->|1.5: finalizeWorkflow()| WF
    Ctrl -->|1.6: exportStandardForms()| Rep

    classDef actor fill:#e0f2fe,stroke:#0284c7,stroke-width:2px;
    classDef boundary fill:#fef3c7,stroke:#d97706,stroke-width:2px;
    classDef control fill:#dbeafe,stroke:#2563eb,stroke-width:2px;
    classDef entity fill:#dcfce7,stroke:#16a34a,stroke-width:2px;

    class Dir actor;
    class UI boundary;
    class Ctrl,WF,Rep control;
    class Pub,Eva entity;
```

#### Dòng thông điệp (Messages Sequence):
- **1 $\to$ 1.2:** Lãnh đạo nhấn phê chuẩn phân loại $\to$ Service kiểm tra tỷ lệ khống chế $N_{\text{Xuất sắc}} \le 0.20 \times N_{\text{Hoàn thành tốt}}$ (**BR-06**).
- **1.3 $\to$ 1.4:** Xác nhận không có vi phạm kỷ luật Đảng/chính quyền (**BR-07**) $\to$ Khóa dữ liệu bất biến mức CSDL (**BR-15**).
- **1.5 $\to$ 1.6:** Hoàn tất luồng workflow $\to$ Tự động sinh trọn bộ Mẫu số 01/02 Nghị định 90.

---

## 4. BẢNG TỔNG HỢP ÁNH XẠ THÔNG ĐIỆP & PHƯƠNG THỨC LỚP

| Biểu đồ | Thông điệp trao đổi (Message) | Đối tượng nhận (Receiver) | Phương thức lớp tương ứng (Method Implementation) |
|---|---|---|---|
| **SD-01** | `postLogin(username, password)` | `AuthController` | `+ authenticate(plainPassword: String): AuthResponse` |
| **SD-01** | `lockAccount(30)` | `User` | `+ lockAccount(durationMinutes: Int): Void` |
| **SD-02** | `validateWeights(items)` | `WeightValidatorService` | `+ validateTotalWeight(items: List<DetailDTO>): Boolean` |
| **SD-02** | `createDetail(...)` | `KPIAssignmentDetail` | `+ createDetail(assignmentId, kpiId, target, weight): Long` |
| **SD-03** | `calculateAchievement(actualVal)` | `KPIAssignmentDetail` | `+ calculateAchievement(actual: Float): Float` |
| **SD-03** | `uploadEvidence(file)` | `FileStorageService` | `+ storeFile(file: MultipartFile): StorageMetadata` |
| **SD-04** | `isScoreGapSignificant()` | `EvaluationDetail` | `+ isScoreGapSignificant(): Boolean` (Kiểm tra $|gap| > 10\%$) |
| **SD-04** | `advanceWorkflow(...)` | `EvaluationWorkflowEngine`| `+ advanceStep(evalId: Long, action: String): WorkflowStep` |
| **SD-05** | `validateDecree90Rules(...)` | `CivilServantClassificationService` | `+ validateExcellenceCap(countGood: Int, countExc: Int): Boolean` |
| **SD-05** | `generateDecreeForms(...)` | `ReportGeneratorService` | `+ generateDecreeForms(periodId: Long, templateCode: String): File` |
| **SD-06** | `queryAggregatedData(...)` | `Evaluation` | `+ getSummaryStats(periodId: Long, orgId: Long): AnalyticsDTO` |
| **SD-08** | `sendMail(to, subject, body)` | `SMTP Mail Server` | `+ sendMimeMessage(message: MimeMessage): Void` |

---

## 5. KẾT LUẬN & CHUẨN BỊ CHO PHIÊN 5

Tài liệu Mô hình Tương tác Phiên 4 đã giải quyết trọn vẹn:
1. **8 Biểu đồ Tuần tự (Sequence Diagrams) chuyên sâu:** Tái hiện chi tiết từng bước trao đổi dữ liệu từ tầng Giao diện (`Boundary`) $\to$ Tầng Điều khiển (`Control`) $\to$ Tầng Thực thể (`Entity`), bao phủ mọi tình huống thành công, rẽ nhánh (`alt`), lặp (`loop`) và ngoại lệ (`exception`).
2. **Tích hợp trọn vẹn các Business Rules:** Thể hiện trực quan vị trí kiểm soát của các quy tắc khắt khe như $\sum \text{Weight} = 100\%$ (**BR-01**), đính kèm minh chứng khi $\ge 120\%$ (**BR-09**), giải trình điều chỉnh lệch $> 10\%$ (**BR-05**), hạ bậc kỷ luật (**BR-07**) và chặn trần khống chế Xuất sắc $\le 20\%$ (**BR-06**).
3. **3 Biểu đồ Giao tiếp (Communication Diagrams):** Cung cấp góc nhìn kiến trúc mạng lưới đối tượng và thứ tự thông điệp phân cấp rõ ràng.

> [!TIP]
> **Đầu vào cho Phiên 5:**  
> Dựa trên các tương tác tuần tự này, **Phiên 5: Mô hình Hoạt động & Trạng thái (Activity & State Machine Model)** sẽ mô hình hóa các luồng nghiệp vụ liên phòng ban bằng **Biểu đồ Phân làn (Swimlane Activity Diagrams)** và xây dựng vòng đời chuyển dịch trạng thái chi tiết của các đối tượng cốt lõi: `KPIAssignment`, `Evaluation`, `EvaluationPeriod` và `UserAccount`.
