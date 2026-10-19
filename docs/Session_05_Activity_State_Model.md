# TÀI LIỆU MÔ HÌNH HOẠT ĐỘNG & TRẠNG THÁI (ACTIVITY & STATE MODEL)
## HỆ THỐNG QUẢN LÝ VÀ ĐÁNH GIÁ KPI CHO DOANH NGHIỆP VÀ CÁN BỘ CÔNG CHỨC
**Mã tài liệu:** ASM-KPI-V1.0  
**Giai đoạn:** Phiên 5 / 10 - Mô hình hóa Hoạt động & Trạng thái (Activity & State Modeling)  
**Tác giả:** Chuyên gia Phân tích Hệ thống (Senior System Analyst)  
**Tài liệu tham chiếu:** [SRS Phiên 1](file:///d:/KPI/docs/Session_01_SRS_Requirements.md) | [Use Case Phiên 2](file:///d:/KPI/docs/Session_02_UseCase_Model.md) | [Class Model Phiên 3](file:///d:/KPI/docs/Session_03_Class_Analysis_Model.md) | [Interaction Model Phiên 4](file:///d:/KPI/docs/Session_04_Interaction_Model.md)  

---

## MỤC LỤC
1. [Giới thiệu Mô hình Hoạt động & Máy trạng thái](#1-giới-thiệu-mô-hình-hoạt-động--máy-trạng-thái)
2. [Hệ thống Biểu đồ Hoạt động Phân làn (Swimlane Activity Diagrams)](#2-hệ-thống-biểu-đồ-hoạt-động-phân-làn-swimlane-activity-diagrams)
   - AD-01: Quy trình tổng thể Đánh giá KPI End-to-End
   - AD-02: Quy trình Phân tầng & Thiết lập KPI Phòng ban (Cascade Goal)
   - AD-03: Quy trình Tự đánh giá & Nộp kết quả (Fork/Join minh chứng song song)
   - AD-04: Quy trình Phê duyệt Đánh giá Đa cấp (Xử lý Từ chối & Thẩm định lại)
   - AD-05: Quy trình Đánh giá Phân loại Cán bộ Công chức theo Nghị định 90/2020/NĐ-CP
   - AD-06: Quy trình Xử lý Dữ liệu & Xuất Báo cáo Đa định dạng
3. [Hệ thống Biểu đồ Máy trạng thái (State Machine Diagrams)](#3-hệ-thống-biểu-đồ-máy-trạng-thái-state-machine-diagrams)
   - SM-01: Máy trạng thái Bản giao KPI (`KPIAssignment`)
   - SM-02: Máy trạng thái Phiếu đánh giá Hiệu suất (`Evaluation`)
   - SM-03: Máy trạng thái Kỳ đánh giá (`EvaluationPeriod`)
   - SM-04: Máy trạng thái Tài khoản Người dùng (`UserAccount`)
4. [Bảng Ma trận Chuyển dịch Trạng thái & Ràng buộc Nghiệp vụ](#4-bảng-ma-trận-chuyển-dịch-trạng-thái--ràng-buộc-nghiệp-vụ)

---

## 1. GIỚI THIỆU MÔ HÌNH HOẠT ĐỘNG & MÁY TRẠNG THÁI

Trong phân tích hệ thống hướng đối tượng:
- **Biểu đồ Hoạt động (Activity Diagram):** Tập trung vào dòng điều khiển (Control Flow) và dòng dữ liệu (Object Flow) của các quy trình nghiệp vụ liên chức năng. Sử dụng cơ chế phân làn (**Swimlanes**) để phân định rạch ròi trách nhiệm giữa 4 vai trò: Ban Nhân sự (`HR`), Quản lý phòng ban (`Manager`), Cán bộ/Nhân viên (`Employee`) và Thủ trưởng cơ quan/Lãnh đạo (`Director`).
- **Biểu đồ Máy trạng thái (State Machine Diagram):** Mô tả chu kỳ sống (Lifecycle) của các thực thể thông tin cốt lõi, xác định các trạng thái bền vững, điều kiện bảo vệ (**Guard Conditions** $[...]$), sự kiện kích hoạt (**Trigger Events**) và các hành vi gắn liền (**Entry / Exit / Do Activities**).

---

## 2. HỆ THỐNG BIỂU ĐỒ HOẠT ĐỘNG PHÂN LÀN (SWIMLANE ACTIVITY DIAGRAMS)

---

### AD-01: QUY TRÌNH TỔNG THỂ ĐÁNH GIÁ KPI (END-TO-END)
- **Mục tiêu:** Mô tả vòng đời khép kín của một chu kỳ đánh giá hiệu suất từ khâu mở kỳ đến khi công bố và lưu trữ kết quả.

```mermaid
flowchart TD
    subgraph HR["Ban Nhân sự / TCCB (HR Admin)"]
        A1((Khởi đầu)) --> A2["Khởi tạo Kỳ đánh giá mới & Cấu hình Deadlines"]
        A2 --> A3["Thiết lập/Cập nhật Thư viện KPI mẫu theo chức danh"]
        A3 --> A4["Thông báo phát động kỳ đánh giá toàn cơ quan"]
    end

    subgraph DIR["Thủ trưởng / Lãnh đạo Đơn vị (Director)"]
        B1["Phê duyệt Bộ chỉ tiêu cấp Tổ chức"]
        B2{"Hội đồng thẩm định đạt điều kiện?"}
        B3["Ký duyệt Quyết định Xếp loại & Phân loại Công chức"]
        B4["Ký số Biên bản & Công bố Kết quả"]
    end

    subgraph MGR["Quản lý Phòng ban (Manager)"]
        C1["Phân rã chỉ tiêu tổ chức xuống KPI Phòng ban"]
        C2["Giao KPI cá nhân & Thiết lập trọng số (Tổng 100%)"]
        C3["Theo dõi tiến độ định kỳ & Nhắc nhở nhân sự"]
        C4["Thẩm định kết quả, đối chiếu minh chứng & Chấm điểm cấp 1"]
        C5{"Lệch điểm > 10%?"}
        C6["Nhập lý do giải trình điều chỉnh (>= 50 ký tự)"]
        C7["Trình hồ sơ đánh giá lên Hội đồng / Lãnh đạo"]
    end

    subgraph EMP["Cán bộ / Nhân viên (Employee)"]
        D1["Xác nhận cam kết chỉ tiêu KPI cá nhân"]
        D2["Cập nhật tiến độ thực hiện định kỳ & Tải minh chứng số"]
        D3["Lập phiếu Tự đánh giá khi cổng mở (Self-assessment)"]
        D4["Nộp phiếu tự đánh giá lên Quản lý trực tiếp"]
        D5["Nhận kết quả chính thức; Gửi khiếu nại (nếu có trong 3 ngày)"]
    end

    A4 --> B1
    B1 --> C1
    C1 --> C2
    C2 --> D1
    D1 --> D2
    D2 --> C3
    C3 --> D3
    D3 --> D4
    D4 --> C4
    C4 --> C5
    C5 -- Có (BR-05) --> C6
    C6 --> C7
    C5 -- Không --> C7
    C7 --> B2
    B2 -- Cần thẩm định lại --> C4
    B2 -- Đạt chuẩn (BR-06) --> B3
    B3 --> B4
    B4 --> D5
    D5 --> A5["Khóa sổ kỳ, xuất báo cáo lương & Lưu trữ hồ sơ"]
    A5 --> A6(((Kết thúc)))

    classDef startEnd fill:#0f172a,stroke:#334155,color:#fff;
    classDef action fill:#f8fafc,stroke:#64748b,stroke-width:1px;
    classDef decision fill:#fef3c7,stroke:#d97706,stroke-width:1px;
    class A1,A6 startEnd;
    class A2,A3,A4,A5,B1,B3,B4,C1,C2,C3,C4,C6,C7,D1,D2,D3,D4,D5 action;
    class B2,C5 decision;
```

---

### AD-02: QUY TRÌNH THIẾT LẬP BỘ KPI CHO PHÒNG BAN & PHÂN TẦNG (CASCADE GOAL)
- **Mục tiêu:** Mô tả quy trình phân rã mục tiêu chiến lược tổ chức $\to$ mục tiêu phòng ban $\to$ chỉ tiêu cá nhân.

```mermaid
flowchart TD
    Start((Bắt đầu)) --> Step1["Tiếp nhận Chỉ tiêu Chiến lược Cấp Đơn vị (Level 1)"]
    Step1 --> Step2["Họp Ban Lãnh đạo Phòng & Xác định Trọng tâm Công tác Kỳ"]
    Step2 --> Step3["Phân rã thành Danh mục KPI Cấp Phòng ban (Level 2)"]
    Step3 --> CheckDeptWeight{"Tổng trọng số KPI Phòng = 100%?"}
    
    CheckDeptWeight -- Không --> FixDept["Cân đối lại trọng số các mục tiêu phòng"]
    FixDept --> Step3
    
    CheckDeptWeight -- Đúng --> Step4["Chiếu theo Ma trận Vị trí Việc làm & Bảng phân công nhiệm vụ"]
    Step4 --> Step5["Gán chỉ tiêu KPI Phòng ban xuống từng Nhân sự (Level 3)"]
    Step5 --> Step6["Kiểm tra tính chất công việc của từng nhân sự"]

    Step6 --> CheckConcurrent{"Nhân sự có kiêm nhiệm không?"}
    CheckConcurrent -- Có (BR-13) --> AllocConcurrent["Tách 2 bản giao: Vị trí chính 70% + Kiêm nhiệm 30%"]
    CheckConcurrent -- Không --> AllocNormal["Gán theo khung năng lực vị trí chuẩn 100%"]

    AllocConcurrent --> ValidateIndiv
    AllocNormal --> ValidateIndiv

    ValidateIndiv{"Kiểm tra từng chỉ tiêu cá nhân:\n5% <= Trọng số <= 50% & Tổng = 100%?"}
    ValidateIndiv -- Vi phạm (BR-01, BR-02) --> AdjustIndiv["Điều chỉnh lại trọng số từng chỉ tiêu"]
    AdjustIndiv --> ValidateIndiv

    ValidateIndiv -- Hợp lệ --> SendStaff["Gửi thông báo yêu cầu Cán bộ/Nhân viên xác nhận cam kết"]
    SendStaff --> StaffReview{"Nhân viên phản hồi đồng thuận?"}

    StaffReview -- Có ý kiến giải trình/khiếu nại --> Dialogue["Đối thoại, trao đổi làm rõ hoặc điều chỉnh kế hoạch"]
    Dialogue --> Step5

    StaffReview -- Đồng ý ký cam kết --> DirectorApproval["Trình Thủ trưởng/Lãnh đạo đơn vị phê chuẩn"]
    DirectorApproval --> FinalCheck{"Lãnh đạo phê duyệt?"}
    FinalCheck -- Yêu cầu sửa đổi --> Step2
    FinalCheck -- Phê chuẩn --> LockPlan["Đóng băng bản giao KPI (Locked Status - BR-03)"]
    LockPlan --> End(((Hoàn thành)))
```

---

### AD-03: QUY TRÌNH TỰ ĐÁNH GIÁ & NỘP KẾT QUẢ (FORK/JOIN XỬ LÝ SONG SONG)
- **Mục tiêu:** Nhân viên cập nhật số liệu, đính kèm minh chứng và tự chấm điểm với cơ chế Fork/Join cho các chỉ tiêu độc lập.

```mermaid
flowchart TD
    Start((Bắt đầu)) --> OpenForm["Mở Phiếu Tự đánh giá Cá nhân của Kỳ"]
    OpenForm --> CheckCutOff{"Kỳ đánh giá còn hạn nộp tự chấm?"}

    CheckCutOff -- Quá hạn (BR-12) --> AutoZero["Hệ thống kích hoạt Auto-Close: Gán điểm tự chấm = 0\nChuyển thẳng hồ sơ cho Quản lý"]
    AutoZero --> EndOverdue(((Kết thúc do trễ hạn)))

    CheckCutOff -- Còn hạn --> LoadData["Tải danh sách các chỉ tiêu KPI đã cam kết đầu kỳ"]
    LoadData --> Fork1["=== FORK: Xử lý song song từng nhóm chỉ tiêu ==="]

    Fork1 --> GroupA["Nhóm 1: Nhập số liệu các chỉ tiêu Định lượng (Doanh số, Dự án, Hồ sơ)"]
    Fork1 --> GroupB["Nhóm 2: Nhập giải trình các chỉ tiêu Định tính (Chất lượng, Đạo đức, Phối hợp)"]

    GroupA --> CalcAuto["Hệ thống tự động tính % hoàn thành theo công thức BR-10 / BR-11"]
    CalcAuto --> CheckEvidence{"Chỉ tiêu có kết quả >= 120%?"}
    
    CheckEvidence -- Có (BR-09) --> ReqUpload["Kích hoạt bắt buộc: Tải tệp biên bản/minh chứng chứng minh"]
    ReqUpload --> UploadFile["Tải lên file PDF/Excel (<= 25MB)"]
    CheckEvidence -- Không --> OptUpload["Tùy chọn tải thêm tài liệu tham khảo"]
    
    UploadFile --> JoinBranch1["Nhánh 1 hoàn tất"]
    OptUpload --> JoinBranch1

    GroupB --> SelfScoreB["Nhập điểm tự chấm thang Likert/Thang 100"]
    SelfScoreB --> JoinBranch2["Nhánh 2 hoàn tất"]

    JoinBranch1 --> Join1["=== JOIN: Hợp nhất toàn bộ dữ liệu chỉ tiêu ==="]
    JoinBranch2 --> Join1

    Join1 --> WriteRemarks["Viết bản tự kiểm điểm, nêu rõ ưu điểm & khuyết điểm trong kỳ"]
    WriteRemarks --> ValidateFinal{"Kiểm tra: Đầy đủ số liệu & Minh chứng hợp lệ?"}

    ValidateFinal -- Thiếu minh chứng bắt buộc --> ShowWarning["Cảnh báo: Bổ sung minh chứng cho các chỉ tiêu vượt trần"]
    ShowWarning --> ReqUpload

    ValidateFinal -- Hợp lệ --> ConfirmSubmit["Xác nhận nộp chính thức (Cam đoan tính chính xác của số liệu)"]
    ConfirmSubmit --> SaveLock["Lưu CSDL, chuyển trạng thái 'Self_Submitted' & Khóa form tự chấm"]
    SaveLock --> SendNotif["Gửi thông báo In-App & Email cho Quản lý trực tiếp"]
    SendNotif --> EndNormal(((Hoàn thành nộp)))
```

---

### AD-04: QUY TRÌNH PHÊ DUYỆT ĐÁNH GIÁ ĐA CẤP (WORKFLOW APPROVAL)
- **Mục tiêu:** Mô tả luồng luân chuyển hồ sơ đánh giá qua các cấp lãnh đạo, xử lý các tình huống từ chối, yêu cầu giải trình bổ sung hoặc phê chuẩn cuối cùng.

```mermaid
flowchart TD
    Start((Bắt đầu)) --> Receive["Tiếp nhận Hồ sơ Đánh giá cấp Phòng ban từ Quản lý"]
    Receive --> StepLevel1["Cấp 1: Phó Giám đốc khối / Lãnh đạo phụ trách thẩm tra hồ sơ"]
    StepLevel1 --> CheckLevel1{"Phó Giám đốc duyệt?"}

    CheckLevel1 -- Trả về yêu cầu thẩm định lại --> RejectToMgr["Chuyển trạng thái 'Returned_For_Review' kèm ý kiến chỉ đạo"]
    RejectToMgr --> NotifyMgr["Gửi thông báo cho Trưởng phòng rà soát lại điểm số"]
    NotifyMgr --> EndReturn(((Trả về cấp dưới)))

    CheckLevel1 -- Đồng thuận phê duyệt --> StepLevel2["Cấp 2: Ban Tổ chức Cán bộ / HR rà soát chuẩn hóa"]
    StepLevel2 --> CheckQuota{"Kiểm tra quỹ khen thưởng & Tỷ lệ chuẩn hóa toàn viện"}

    CheckQuota -- Sai lệch chỉ tiêu / Lạm phát điểm --> Consult["Họp trao đổi liên phòng ban để thống nhất mức điểm"]
    Consult --> StepLevel1

    CheckQuota -- Đạt chuẩn --> StepLevel3["Cấp 3: Thủ trưởng Cơ quan / Tổng Giám đốc xem xét"]
    StepLevel3 --> CheckDir{"Thủ trưởng phê chuẩn kết quả?"}

    CheckDir -- Không phê duyệt --> ChairCouncil["Triệu tập Hội đồng Thi đua - Khen thưởng họp bình xét lại"]
    ChairCouncil --> StepLevel2

    CheckDir -- Phê duyệt chính thức --> DigitalSign["Thực hiện Ký số Quyết định Phê chuẩn"]
    DigitalSign --> Immutability["Kích hoạt khóa bất biến dữ liệu quá khứ (BR-15: Read-Only CSDL)"]
    Immutability --> DispatchResults["Hệ thống tự động phát hành Bảng điểm chính thức cho Cán bộ/Nhân viên"]
    DispatchResults --> StartAppealWindow["Mở thời hạn tiếp nhận khiếu nại (03 ngày làm việc theo BR-14)"]
    StartAppealWindow --> EndApproved(((Hoàn thành phê duyệt)))
```

---

### AD-05: QUY TRÌNH ĐÁNH GIÁ PHÂN LOẠI CÁN BỘ CÔNG CHỨC THEO NGHỊ ĐỊNH 90
- **Mục tiêu:** Quy chuẩn hóa 4 bước đánh giá phân loại cán bộ, công chức, viên chức theo đúng quy định tại Nghị định số 90/2020/NĐ-CP và Nghị định 48/2023/NĐ-CP.

```mermaid
flowchart TD
    Start((Bắt đầu)) --> Phase1["BƯỚC 1: Cán bộ công chức làm Báo cáo tự đánh giá, xếp loại kết quả công tác"]
    Phase1 --> Phase2["BƯỚC 2: Tổ chức Cuộc họp lấy ý kiến nhận xét của Cấp ủy & Tập thể nơi công tác"]
    Phase2 --> CollectComments["Thư ký Hội đồng tổng hợp Ý kiến đóng góp & Biên bản họp chi bộ"]

    CollectComments --> Phase3["BƯỚC 3: Hội đồng Thẩm định & Tham mưu giúp việc rà soát hồ sơ"]
    Phase3 --> CheckDiscipline{"Cán bộ có bị kỷ luật Đảng hoặc Hành chính trong năm?"}

    CheckDiscipline -- Có bị kỷ luật (BR-07) --> ForceFail["Tự động áp dụng Điểm liệt: Khóa các mức trên\nÉp phân loại 'Không hoàn thành nhiệm vụ'"]
    ForceFail --> Phase4

    CheckDiscipline -- Không bị kỷ luật --> CheckScores["Tổng hợp Điểm KPI chuyên môn + Điểm Phẩm chất chính trị, Đạo đức, Tác phong"]
    CheckScores --> CheckTiers{"Điểm tổng hợp đạt ngưỡng nào?"}

    CheckTiers -- Điểm < 50% --> RankD["Đề xuất: Không hoàn thành nhiệm vụ"]
    CheckTiers -- 50% <= Điểm < 75% --> RankC["Đề xuất: Hoàn thành nhiệm vụ"]
    CheckTiers -- 75% <= Điểm < 90% --> RankB["Đề xuất: Hoàn thành tốt nhiệm vụ"]
    CheckTiers -- Điểm >= 90% --> CheckInitiative{"Có Đề tài khoa học / Sáng kiến cải tiến được công nhận?"}

    CheckInitiative -- Không có sáng kiến (BR-08) --> RankB_Fallback["Hạ về mức: Hoàn thành tốt nhiệm vụ (Do thiếu sáng kiến)"]
    CheckInitiative -- Đầy đủ sáng kiến --> RankA["Đề xuất: Hoàn thành xuất sắc nhiệm vụ"]

    RankD --> MergeTiers["Tổng hợp Danh sách đề xuất toàn Cơ quan"]
    RankC --> MergeTiers
    RankB --> MergeTiers
    RankB_Fallback --> MergeTiers
    RankA --> MergeTiers

    MergeTiers --> Phase4["BƯỚC 4: Thủ trưởng cơ quan xem xét, quyết định phân loại chất lượng"]
    Phase4 --> Check20Cap{"Kiểm soát tỷ lệ (BR-06):\nN(Xuất sắc) <= 0.20 * N(Hoàn thành tốt)?"}

    Check20Cap -- Vượt quá 20% (Vi phạm luật) --> Rebalance["Khóa nút Duyệt. Yêu cầu Hội đồng bình bầu chọn lọc lại danh sách Xuất sắc"]
    Rebalance --> Phase4

    Check20Cap -- Đạt chuẩn <= 20% --> SignDecree["Thủ trưởng ký ban hành Quyết định Xếp loại Chất lượng Cán bộ"]
    SignDecree --> AutoExport["Tự động trích xuất Phiếu Mẫu số 01 / Mẫu số 02 lưu vào Hồ sơ Cán bộ"]
    AutoExport --> End(((Hoàn thành công tác năm)))
```

---

### AD-06: QUY TRÌNH TẠO & XUẤT BÁO CÁO TỔNG HỢP
- **Mục tiêu:** Lựa chọn mẫu, áp bộ lọc động, truy xuất CSDL đa bảng, tính toán thống kê và trích xuất file Excel/PDF.

```mermaid
flowchart TD
    Start((Bắt đầu)) --> SelectModule["Người dùng vào phân hệ 'Báo cáo & Phân tích'"]
    SelectModule --> ChooseTemplate["Lựa chọn Mẫu báo cáo (Tổng hợp KPI kỳ / Bảng lương 3P / Mẫu 01-02 NĐ 90 / Xếp hạng)"]
    ChooseTemplate --> SetFilters["Thiết lập Tham số lọc: Kỳ đánh giá, Đơn vị, Phòng ban, Khối chức danh, Xếp loại"]
    SetFilters --> ClickGenerate["Nhấn nút 'Truy xuất Báo cáo'"]

    ClickGenerate --> QueryCache{"Dữ liệu đã có trong Redis Cache?"}
    QueryCache -- Cache Hit --> FetchCache["Lấy tức thời dữ liệu tổng hợp từ Cache"]
    QueryCache -- Cache Miss --> QueryDB["Truy vấn CSDL (Kết nối các bảng: Evaluations, Users, Departments, KPIs)"]
    QueryDB --> RunMath["Thực thi tính toán: Trung bình cộng, Phân vị, Tỷ lệ hoàn thành, Xếp hạng"]
    RunMath --> SaveCache["Lưu kết quả tạm thời vào Cache (TTL = 15 phút)"]
    SaveCache --> FetchCache

    FetchCache --> RenderUI["Hiển thị Bảng dữ liệu tương tác & Biểu đồ trực quan trên Web (< 1.5s)"]
    RenderUI --> ExportPrompt{"Người dùng có yêu cầu xuất file?"}

    ExportPrompt -- Không (Chỉ xem) --> EndView(((Kết thúc xem)))
    ExportPrompt -- Chọn Xuất File --> SelectFormat{"Chọn định dạng xuất"}

    SelectFormat -- Excel (XLSX) --> BuildExcel["Khởi tạo Workbook, format cell, chèn công thức tính toán tự động"]
    SelectFormat -- PDF (In ấn) --> BuildPDF["Áp mẫu Template CSS, phân trang, chèn chữ ký số & watermark cơ quan"]
    SelectFormat -- CSV (Tích hợp) --> BuildCSV["Trích xuất dữ liệu thô định dạng UTF-8 CSV"]

    BuildExcel --> CheckSize{"Kích thước tệp xuất?"}
    BuildPDF --> CheckSize
    BuildCSV --> CheckSize

    CheckSize -- Nhỏ (< 5.000 dòng) --> DirectDownload["Tải trực tiếp về trình duyệt người dùng (< 5 giây)"]
    CheckSize -- Lớn (>= 5.000 dòng) --> AsyncJob["Đẩy tác vụ xử lý nền (Background Worker); gửi email đính kèm link tải sau"]

    DirectDownload --> EndExport(((Hoàn thành xuất)))
    AsyncJob --> EndExport
```

---

## 3. HỆ THỐNG BIỂU ĐỒ MÁY TRẠNG THÁI (STATE MACHINE DIAGRAMS)

---

### SM-01: MÁY TRẠNG THÁI BẢN GIAO KPI (`KPIAssignment`)
- **Đối tượng:** Thực thể `KPIAssignment`
- **Mục tiêu:** Quản lý vòng đời từ bản nháp thiết lập chỉ tiêu đến khi khóa dữ liệu và lưu trữ lịch sử.

```mermaid
stateDiagram-v2
    [*] --> Draft : Khởi tạo bản giao

    state Draft {
        [*] --> Editing
        Editing : entry / loadKPITemplates()
        Editing : do / configureTargetsAndWeights()
        Editing : exit / checkDraftStatus()
    }

    Draft --> Pending_Staff_Confirm : submitAssignment() [totalWeight == 100% && 5% <= weight <= 50%]\n/ notifyStaff()
    
    Pending_Staff_Confirm --> Draft : requestRevision() [Nhân viên có ý kiến phản hồi]\n/ sendFeedbackToManager()
    Pending_Staff_Confirm --> Pending_Approval : staffCommit() [Nhân viên ký cam kết mục tiêu]\n/ notifyDirector()

    Pending_Approval --> Draft : directorReject() [Lãnh đạo yêu cầu chỉnh sửa]\n/ logReason()
    Pending_Approval --> Approved_Locked : directorApprove() [Lãnh đạo phê chuẩn]\n/ lockData()

    state Approved_Locked {
        [*] --> Active
        Active : entry / readyForExecution()
        Active --> In_Evaluation : periodOpenEvaluation() [Mở cổng kỳ đánh giá]
        In_Evaluation : do / recordActualProgress()
    }

    In_Evaluation --> Completed : finalApproved() [Hoàn thành kỳ đánh giá]\n/ freezeScores()
    Completed --> Archived : archivePeriod() [Đóng sổ kỳ]\n/ exportAnnualRecord()
    Archived --> [*]
```

#### Bảng chi tiết trạng thái của `KPIAssignment`:
| Tên trạng thái | Ý nghĩa nghiệp vụ | Quyền thao tác | Ràng buộc nghiệp vụ liên quan |
|---|---|---|---|
| `Draft` | Quản lý đang thiết lập chỉ tiêu, chưa gửi nhân sự. | Quản lý (CRUD) | BR-01, BR-02 |
| `Pending_Staff_Confirm` | Đã gửi nhân sự, chờ nhân sự xem và bấm cam kết. | Nhân viên (Commit/Feedback) | BR-13 (Kiểm tra kiêm nhiệm) |
| `Pending_Approval` | Nhân viên đã cam kết, chờ Lãnh đạo cơ quan duyệt. | Lãnh đạo (Approve/Reject) | - |
| `Approved_Locked` | Đã duyệt. Kế hoạch đóng băng, nhân viên bắt đầu thực hiện. | Read-Only | BR-03 (Lock on Submit) |
| `In_Evaluation` | Kỳ đánh giá mở cổng, bản giao sẵn sàng để nộp kết quả. | Nhân viên (Self-assess) | BR-12 (Kiểm soát hạn chót) |
| `Completed` | Kết quả đánh giá đã được chốt, sẵn sàng tính thưởng. | Read-Only | BR-20 (Thang điểm 100) |
| `Archived` | Dữ liệu lưu trữ lịch sử vĩnh viễn trong kho lưu trữ. | Read-Only (Thanh tra/Audit) | BR-15 (Data Immutability) |

---

### SM-02: MÁY TRẠNG THÁI PHIẾU ĐÁNH GIÁ HIỆU SUẤT (`Evaluation`)
- **Đối tượng:** Thực thể `Evaluation`
- **Mục tiêu:** Quản lý quy trình chấm điểm qua các cấp, kiểm tra điều chỉnh lệch điểm và xử lý khiếu nại.

```mermaid
stateDiagram-v2
    [*] --> Not_Started : Mở kỳ đánh giá

    Not_Started --> Self_Assessing : openPortal() / notifyStaff()
    Not_Started --> Forced_Zero_To_Manager : autoCloseCron() [Quá hạn nộp tự chấm - BR-12]\n/ setSelfScore(0)

    state Self_Assessing {
        [*] --> EnteringActuals
        EnteringActuals : do / calculateFormulas()
        EnteringActuals --> UploadingEvidence : actual >= 120% [BR-09]
        UploadingEvidence --> ReadyToSubmit : evidenceUploaded == true
    }

    Self_Assessing --> Self_Submitted : submitSelfAssessment() / notifyManager()
    Forced_Zero_To_Manager --> Manager_Reviewing : managerOpen()

    Self_Submitted --> Manager_Reviewing : managerOpen()

    state Manager_Reviewing {
        [*] --> CheckingEvidences
        CheckingEvidences : do / verifyProofs()
        CheckingEvidences --> Scoring
        Scoring : entry / compareWithSelfScores()
        Scoring : do / requireReasonIfGapOver10Percent() [BR-05]
    }

    Manager_Reviewing --> Returned_To_Staff : requestClarification() [Yêu cầu bổ sung minh chứng]\n/ open24hWindow()
    Returned_To_Staff --> Self_Submitted : resubmitWithProof()

    Manager_Reviewing --> Pending_Director_Approval : managerSubmit() [validScoreGaps == true]\n/ calculateManagerTotal()

    Pending_Director_Approval --> Manager_Reviewing : directorSendBack() [Yêu cầu rà soát lại]\n/ logInstructions()
    Pending_Director_Approval --> Final_Approved : directorApprove() [Khống chế Xuất sắc <= 20% - BR-06]\n/ signAndPublish()

    state Final_Approved {
        [*] --> Appeal_Window_Open
        Appeal_Window_Open : entry / start72HoursCountdown() [BR-14]
        Appeal_Window_Open --> Under_Dispute : staffDispute() [Trong vòng 3 ngày làm việc]
        Under_Dispute --> Appeal_Resolved : resolveDispute() [Hội đồng kết luận]
        Appeal_Resolved --> Appeal_Window_Closed
        Appeal_Window_Open --> Appeal_Window_Closed : timerExpired() [Hết 72h không khiếu nại]
    }

    Final_Approved --> Locked : closeAndLock() / setReadOnly() [BR-15]
    Locked --> [*]
```

#### Bảng chi tiết trạng thái của `Evaluation`:
| Trạng thái | Mô tả & Điều kiện kích hoạt | Sự kiện kế tiếp |
|---|---|---|
| `Not_Started` | Kỳ đánh giá mở cổng nhưng nhân viên chưa mở form. | Nhân viên mở form $\to$ `Self_Assessing`. |
| `Self_Assessing` | Nhân viên đang nhập số liệu và tải minh chứng. | Nhấn nộp $\to$ `Self_Submitted`. |
| `Forced_Zero_To_Manager` | Nhân viên quá hạn không nộp (BR-12), hệ thống ép tự chấm 0 điểm. | Chuyển thẳng $\to$ `Manager_Reviewing`. |
| `Self_Submitted` | Nhân viên đã nộp bản tự chấm, chờ Quản lý mở chấm. | Quản lý mở $\to$ `Manager_Reviewing`. |
| `Manager_Reviewing` | Quản lý đang thẩm tra minh chứng và chấm điểm. | Chấm xong $\to$ `Pending_Director_Approval`. |
| `Returned_To_Staff` | Quản lý yêu cầu bổ sung minh chứng số trong 24 giờ. | Bổ sung nộp lại $\to$ `Self_Submitted`. |
| `Pending_Director_Approval` | Quản lý đã chấm xong, chờ Lãnh đạo cơ quan ký duyệt. | Ký duyệt $\to$ `Final_Approved`. |
| `Final_Approved` | Lãnh đạo đã duyệt. Mở thời hạn 72h cho nhân viên khiếu nại (BR-14). | Hết 72h $\to$ `Locked`. Khiếu nại $\to$ `Under_Dispute`. |
| `Under_Dispute` | Đang trong quá trình thụ lý giải quyết đơn khiếu nại kết quả. | Xử lý xong $\to$ `Appeal_Resolved`. |
| `Locked` | Dữ liệu đóng băng vĩnh viễn, chuyển sang chế độ Read-Only (BR-15). | Kết thúc vòng đời. |

---

### SM-03: MÁY TRẠNG THÁI KỲ ĐÁNH GIÁ (`EvaluationPeriod`)
- **Đối tượng:** Thực thể `EvaluationPeriod`
- **Mục tiêu:** Quản lý vòng đời chu kỳ đánh giá toàn cơ quan, điều phối các mốc mở/đóng cổng tự động.

```mermaid
stateDiagram-v2
    [*] --> Planning : Tạo mới kỳ đánh giá

    state Planning {
        [*] --> SettingDeadlines
        SettingDeadlines : do / configureTimeWindows()
        SettingDeadlines --> ReadyToOpen : templatesAssigned == true
    }

    Planning --> Open : activatePeriod() [currentDate >= startDate]\n/ sendNotificationToAll()

    state Open {
        [*] --> SelfAssessmentPhase
        SelfAssessmentPhase : entry / openStaffPortals()
        SelfAssessmentPhase --> ManagerReviewPhase : currentDate > selfDeadline\n/ autoCloseSelfAndNotifyManagers() [BR-12]
        ManagerReviewPhase : entry / openManagerPortals()
        ManagerReviewPhase --> CouncilApprovalPhase : currentDate > managerDeadline\n/ alertCouncil()
    }

    Open --> Closing : startClosingProcess() [Hội đồng họp phân loại xong]
    
    state Closing {
        [*] --> VerifyingQuotas
        VerifyingQuotas : do / checkDecree90Cap() [BR-06: Xuất sắc <= 20%]
        VerifyingQuotas --> SigningResults : isQuotaValid == true
    }

    Closing --> Closed : closePeriod() [Lãnh đạo ký quyết định]\n/ freezeAllEvaluations() [BR-15]
    Closed --> Archived : archivePeriod() [Đồng bộ sang HRM/Lương]\n/ backupDatabase()
    Archived --> [*]
```

---

### SM-04: MÁY TRẠNG THÁI TÀI KHOẢN NGƯỜI DÙNG (`UserAccount`)
- **Đối tượng:** Thực thể `User`
- **Mục tiêu:** Quản lý an ninh tài khoản, kiểm soát đăng nhập sai, khóa tự động và kích hoạt phiên.

```mermaid
stateDiagram-v2
    [*] --> Pending_Activation : Tạo tài khoản mới từ HRM

    Pending_Activation --> Active : firstTimeSetup() [Đổi mật khẩu & Kích hoạt 2FA]\n/ activateUser()
    
    state Active {
        [*] --> Idle
        Idle --> Authenticating : loginAttempt()
        Authenticating --> Authenticated : passwordMatch == true / resetFailedCount()
        Authenticated --> Idle : logout()
    }

    Active --> Locked : failedAttempts >= 5 [BR-Security / NFR-SEC-01]\n/ lockTemporary(30m) & sendAlertEmail()
    
    Locked --> Active : timerExpired() [Hết 30 phút]\n/ resetToActive()
    Locked --> Active : adminUnlock() [Quản trị viên mở khóa thủ công]

    Active --> Suspended : adminSuspend() [Nhân viên tạm đình chỉ công tác/nghỉ việc]\n/ revokeAllSessions()
    Suspended --> Active : adminReactivate()

    Suspended --> Deactivated : terminateEmployment() [Chấm dứt hợp đồng lao động / Thôi việc]\n/ softDelete()
    Deactivated --> [*]
```

---

## 4. BẢNG MA TRẬN CHUYỂN DỊCH TRẠNG THÁI & RÀNG BUỘC NGHIỆP VỤ

Bảng đối soát chi tiết giữa các sự kiện chuyển trạng thái và các Ràng buộc nghiệp vụ (**BR**) đã được quy định:

| Đối tượng (Entity) | Trạng thái nguồn (From) | Trạng thái đích (To) | Sự kiện kích hoạt (Event) | Điều kiện bảo vệ (Guard Condition) | Hành vi thực thi (Action) | Ràng buộc ánh xạ |
|---|---|---|---|---|---|---|
| `KPIAssignment` | `Draft` | `Pending_Staff_Confirm` | `submitAssignment()` | $\sum \text{Weight} = 100\%$ và $5\% \le \text{Weight}_i \le 50\%$ | Gửi thông báo đến nhân viên | **BR-01, BR-02** |
| `KPIAssignment` | `Pending_Approval`| `Approved_Locked` | `directorApprove()` | Lãnh đạo xác nhận phê chuẩn | Đóng băng dữ liệu chỉ tiêu | **BR-03** |
| `KPIAssignment` | `Pending_Staff_Confirm`| `Draft` | `requestRevision()` | Nhân viên kiêm nhiệm phản hồi tỷ lệ | Mở lại chế độ chỉnh sửa | **BR-13** |
| `Evaluation` | `Not_Started` | `Forced_Zero_To_Manager`| `autoCloseCron()` | $\text{CurrentDate} > \text{SelfDeadline}$ | Gán điểm tự chấm $= 0$ | **BR-12** |
| `Evaluation` | `Self_Assessing` | `Self_Submitted` | `submitSelfAssessment()`| Các chỉ tiêu đạt $\ge 120\%$ bắt buộc có minh chứng số | Khóa form tự chấm, báo Quản lý | **BR-09, BR-10, BR-11** |
| `Evaluation` | `Manager_Reviewing`| `Pending_Director_Approval`| `managerSubmit()`| Lệch điểm $> 10\%$ bắt buộc có lý do $\ge 50$ ký tự | Tính điểm tổng theo thang 100 | **BR-05, BR-20** |
| `Evaluation` | `Pending_Director_Approval`| `Final_Approved`| `directorApprove()` | $N_{\text{Xuất sắc}} \le 0.20 \times N_{\text{Hoàn thành tốt}}$ và không có kỷ luật | Ký số, mở hạn khiếu nại 72h | **BR-06, BR-07, BR-08, BR-14** |
| `Evaluation` | `Final_Approved` | `Locked` | `closeAndLock()` | Hết 72h làm việc kể từ lúc công bố | Chuyển chế độ Read-Only vĩnh viễn | **BR-15** |
| `User` | `Active` | `Locked` | `loginAttempt()` | `failedAttempts >= 5` | Khóa 30 phút, gửi email cảnh báo | **NFR-SEC-01** |

---

## 5. KẾT LUẬN & CHUẨN BỊ CHO PHIÊN 6

Tài liệu Mô hình Hoạt động & Trạng thái Phiên 5 đã hoàn thành:
1. **6 Biểu đồ Hoạt động Phân làn (Swimlanes):** Làm rõ trách nhiệm của từng Actor (`HR`, `Manager`, `Employee`, `Director`), tích hợp đầy đủ các nút Quyết định (`Decision Nodes`), Rẽ nhánh/Hợp nhất (`Fork/Join`) và xử lý ngoại lệ trễ hạn.
2. **4 Biểu đồ Máy trạng thái (State Machine):** Chuẩn hóa vòng đời và sự chuyển dịch của các thực thể cốt lõi (`KPIAssignment`, `Evaluation`, `EvaluationPeriod`, `UserAccount`) bằng cú pháp `Mermaid stateDiagram-v2`.
3. **Tích hợp toàn diện các Business Rules:** Thể hiện trực quan các điểm kiểm soát tự động của hệ thống (khống chế xuất sắc 20%, khóa bất biến dữ liệu, bắt buộc giải trình, tự động đóng kỳ).

> [!TIP]
> **Đầu vào cho Phiên 6:**  
> Dựa trên các thuộc tính của Lớp Thực thể (Phiên 3) và các trạng thái dữ liệu (Phiên 5), **Phiên 6: Thiết kế Cơ sở Dữ liệu (Database Architecture & Physical Design)** sẽ xây dựng **Biểu đồ ERD chi tiết**, thiết kế hơn 30 bảng CSDL vật lý (chuẩn 3NF), định nghĩa toàn bộ khóa chính (PK), khóa ngoại (FK), Indexing, các trường Audit Trail và viết mã kịch bản SQL DDL hoàn chỉnh tương thích PostgreSQL / SQL Server.
