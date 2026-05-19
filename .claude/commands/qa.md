你要對目前未提交的變更做提交前審查。

這個審查必須用**乾淨 context** 進行 — 在主對話裡審查你自己剛寫的 code 會有 confirmation bias，容易放過問題。所以不要自己直接審。

請用 Agent 工具啟動 `qa-reviewer` sub-agent 來執行審查。把以下使用者補充說明（如果有）原樣傳給它當聚焦上下文：

$ARGUMENTS

`qa-reviewer` 會自己跑 `git status` / `git diff`、讀檔、依八大清單逐項審查，並回傳結構化報告。

拿到報告後原樣呈現給使用者，不要自己改寫結論或放寬判定。如果使用者要針對報告中某項深入討論，再用主 context 接續。
