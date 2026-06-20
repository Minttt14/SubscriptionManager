# 期末專題-訂閱管理小幫手
## 主要功能

本程式為一款訂閱服務追蹤與財務規劃工具，其核心特色如下：

- 智慧到期提醒：系統自動計算各項訂閱的扣款日期，並依據緊急程度提供視覺化提示，確保您不再錯過繳費期限
- 自動化財務統計：無需手動記帳，系統透過內建邏輯自動結算已發生的費用，並統計每月開銷
- 週期性結算處理：針對「每月」、「每季」、「每年」不同週期，系統會自動處理大小月（如 2 月 28/29 日）的問題，確保日期計算精準
- 視覺化分析：透過圓餅圖與趨勢比較，讓您直觀地掌握各項訂閱的支出佔比，優化個人開銷配置
- 資料安全防護：使用者密碼經 SHA-256 加密處理，確保個人帳戶安全

## 使用方法
### 一、 帳號存取流程：登入與註冊
#### 1. 註冊新帳號
首次使用者，請在登入畫面點擊「沒有帳號？立即註冊」建立個人帳號：

<img width="400" height="300" alt="螢幕擷取畫面 2026-06-20 163323" src="https://github.com/user-attachments/assets/0d66091c-5bc3-4280-a082-a2daf643ef21" />
<br>

#### 防呆與安全機制：
- 密碼長度限制： 系統強制要求密碼長度至少為 6 個字元
- 帳號查重： 系統會自動查詢資料庫，確保該帳號未被註冊過
- 輸入密碼一致性： 兩次輸入的密碼要相同

<img width="300" height="200" alt="螢幕擷取畫面 2026-06-20 163537" src="https://github.com/user-attachments/assets/fdc52246-4e61-40a0-9628-033c8231ad31" />
<img width="300" height="200" alt="螢幕擷取畫面 2026-06-20 163746" src="https://github.com/user-attachments/assets/d31dda7d-df6b-4e27-a2d4-597a12316c30" />
<img width="300" height="200" alt="螢幕擷取畫面 2026-06-20 163833" src="https://github.com/user-attachments/assets/e275a38a-0f5b-4a6b-9d57-023805a310be" />

#### 2. 系統登入
輸入正確的帳號與密碼，點擊「登入」

<img width="400" height="300" alt="螢幕擷取畫面 2026-06-20 163310" src="https://github.com/user-attachments/assets/2bf1537f-a226-4661-aa96-f8f358e825b7" />

#### 機制說明：
- 身分驗證： 驗證帳號密碼，通過後儲存「登入狀態」
- 錯誤處理： 若輸入錯誤，系統會自動清除密碼欄位並要求重新輸入

<img width="300" height="200" alt="螢幕擷取畫面 2026-06-20 164323" src="https://github.com/user-attachments/assets/17272052-7f82-4187-93a8-969862ac2375" />

<br>

### 二、 主頁面
左上角會顯示登入的帳號名，並提供多個導覽頁面可供切換

<img width="400" height="280" alt="image" src="https://github.com/user-attachments/assets/014765af-0a28-4305-8003-47fbd78f0bc1" />

### 三、 即將扣款通知 
系統會自動篩選未來 7 天內的繳費任務，並依據到期日的臨近程度進行排序並透過不同顏色分級
- 紅色卡片：剩餘 3 天內
- 橘色卡片：剩餘 4-5 天
- 綠色卡片：剩餘6-7 天

<img width="400" height="260" alt="image" src="https://github.com/user-attachments/assets/049cd87f-3f2e-4d5d-80d4-1060fbaebed4" />

### 四、 訂閱管理功能
#### 1. 新增訂閱項目
點擊「＋新增」按鈕，輸入名稱、金額、扣款週期、開始日期及提醒天數

<img width="400" height="450" alt="螢幕擷取畫面 2026-06-20 183411" src="https://github.com/user-attachments/assets/74512e75-f8c3-412c-80e5-b000d2cf6919" />

#### 防呆機制：
- 日期防呆： 系統不允許輸入「未來的日期」作為開始訂閱日
- 欄位檢查： 確保金額與日期格式正確
- 提醒日上限： 提醒日不得早於30 天

<img width="300" height="180" alt="螢幕擷取畫面 2026-06-20 183503" src="https://github.com/user-attachments/assets/7b063d09-5e2d-408a-91f6-aae2b7d7756b" />
<img width="300" height="180" alt="螢幕擷取畫面 2026-06-20 183550" src="https://github.com/user-attachments/assets/7c74fc1e-4bec-4c33-bec8-da183151edbe" />



