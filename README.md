# Rimworld Stripper Pole (Forked)

This is a forked and extended version of the mod originally created by cryptidfarmer.  
[Original Repository (GitLab)](https://gitgud.io/cryptidfarmer/rimworld-stripper-pole)  
*I will remove this repository if requested by the original author.*

---

## Requirements

- **[UAP (Ultimate Animation Pack)](https://github.com/Teacher/ultimate-animation-pack)**
- **Rimworld-Animations 2.0**
- **RJW (RimJobWorld)**
- **[Hospitality (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3509486825)** *(Required for customer invitation & guest management)*
- *(Optional)* **[NL] Facial Animation - WIP** *(Facial expressions during dance, invitation, and interactions)*

---

## Features

### 1. Customer Invitation System (Hospitality Integration)
- **Manual Invitation**: Right-click on an assigned stripper pole to invite visiting Hospitality guests to watch a dance performance (requires the pole to have a single assigned owner).
- **Autonomous Dancer Work**: Adds the **Stripper** work type (`StripperPole_Dancer`). Set it to high priority in the Work tab, and the pawn will autonomously seek out guests and invite them to watch.
- **Configurable Behavior**: Custom cooldown timers, wander duration while seeking guests, and beauty-scaled acceptance chance.

### 2. Prostitution Negotiation & Payouts
- **Choose Interactions**: When a dance concludes, an interested spectator may offer silver. An interactive negotiation dialog allows you to choose the specific sexual interaction.
- **Silver Economics**: Base dance and prostitution fees, beauty-based multipliers, and act-specific multipliers can all be configured.
- **Auto-Spawn Silver**: Optional settings to generate silver out of thin air if a guest cannot afford the full dance or prostitution fee.

### 3. Facial Animation Integration
- Supports **[NL] Facial Animation** to give pawns dynamic facial expressions during dance invitations, performances, and subsequent interactions (works seamlessly even if the mod is not installed).

### 4. Rich Mod Settings
All values are configurable via **Options -> Mod Options -> Stripper Pole**:
- Base dance price & beauty multiplier
- Base prostitution price & beauty multiplier
- Position / sex-type multipliers (Vaginal, Anal, Oral, Double Penetration, Boobjob, Handjob, Footjob, Fingering, Scissoring, Mutual Masturbation, Fisting, Rimming, Fellatio, Cunnilingus, Sixty-Nine)
- Silver auto-spawn toggles
- Customer invitation cooldown & wander duration
- Dance cooldown when guests are present

---

## Bug Fixes

- **Post-Dance Movement Stack**: Fixed an issue where dancers became permanently stuck after dancing due to UAP position locking not being released on single-pawn stop calls.
- **UAP Arrival Teleportation**: Fixed an issue where pawns were forcibly teleported to the pole location if drafted while pole dancing or approaching the pole.
- **Joy Gain Fix**: Fixed an issue from the original mod where neither the dancer nor spectators were gaining recreation (joy) during performances.
- **Reservation Continuity**: Fixed reservation breaks between customer invitation, dancing, and prostitution transitions.
- **Prostitution Dialog Failsafe**: Added safety guards to prevent pawns from freezing permanently if negotiation dialogs were closed unexpectedly.
- **Mote / Effect Error Spam**: Fixed NullReferenceExceptions in dance lights and rotation mote handling.
- **Beauty Multiplier Zero Handling**: Fixed fee calculation becoming zero when pawn beauty was zero.

---

## 以下日本語

こちらのModはcryptidfarmerさん作のModに機能拡張およびバグ修正を行ったフォーク版です。  
[元リポジトリ (GitLab)](https://gitgud.io/cryptidfarmer/rimworld-stripper-pole)  
*元Modのフォークであるため、削除申請があれば削除します。*

### 前提Mod
- **UAP (Ultimate Animation Pack)**
- **Rimworld-Animations 2.0**
- **RJW (RimJobWorld)**
- **[Hospitality (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3509486825)**（客の招待・管理に必要）
- *(任意)* **[NL] Facial Animation - WIP**（客呼び中、ダンス中、行為中の表情変化）

### 主な追加機能
1. **客の呼び込みシステム (Hospitality連携)**
   - **手動呼び出し**: ポールを右クリックして訪問客にダンスを観に来るようアプローチ（ポール所有者が1人の場合に実行可能）。
   - **自発的な仕事**: 優先順位タブに「ストリップ」職業が追加され、高優先度にしておくことで訪問客がいる際に自発的に客を誘って踊ります。
2. **売春交渉システム**
   - ダンス終了後に客がチップ・シルバーを提示し、行為の選択ダイアログが表示されます。
   - 体位・行為ごとの価格倍率や、美しさに応じたチップ分配。
   - 客のシルバーが足りない場合に強制生成するオプション設定。
3. **Mod設定による詳細カスタマイズ**
   - ダンス/売春の基本料金および美しさ倍率
   - 各体位（マンコ、アナル、口、二穴、パイズリ、手コキ、足コキ、手マン、具合わせ、相互オナニー、フィスト、アナル舐め、フェラ、クンニ、シックスナイン）の倍率設定
   - 客呼び込みのクールタイム、うろつき時間、承諾確率設定
   - 通常ダンスのクールタイム設定
4. **[NL] Facial Animation 対応**
   - 客呼び中、踊り中、行為中での表情アニメーションに対応（未導入環境でも動作します）。

### 主なバグ修正
- **ダンス終了後の移動スタック修正**: UAPの位置拘束（`AnimationPositionLock`）が解除されず、ダンス後にその場で固まる不具合を根本修正。
- **UAP到着テレポート防止**: ポールダンス実行時や到着時に徴兵などを行うと強制テレポートしてしまう問題を修正。
- **娯楽ゲージ増加の修正**: 元Modでダンサーと観客ともに娯楽（Joy）が増加していなかった不具合を修正。
- **予約（Reserve）の連続性**: 客呼び→ダンス→行為のフローで予約が途切れて中断される問題を修正。
- **売春ダイアログのフェイルセーフ**: ウィンドウが閉じられた際や例外発生時にポーンが永久停止しないよう安全処理を追加。
- **MoteエフェクトのNull参照修正**: ダンスライト等のMote生成時のエラーログを解消。

---

## Original Readme (by cryptidfarmer)

A Rimworld mod that adds a stripper pole.
* Stripper pole construction requires Smithing and takes metal or wood to construct
* Pawns can be commanded to do a dance on the pole
* Pawns can also seek out the pole to dance for recreation
* Dancing pawns will take off clothes and gain joy
* Other pawns can go watch a dance for recreation
* Pawns that watch a dance can gain an opinion change based on the beauty of the dancer
* Dance Spot: smaller range, smaller bonuses, but doesn't require anything to make

Coming Soon™️ (In no particular order):
* Ideology Precepts: Like dancing, Don't like dancing
* Exhibitionist Bonuses: extra joy for doing a dance
* Ideology Ritual: Bring everyone over to watch a dance
* Anomaly Ritual: Multiple people dance at the same time
* Hospitality/Brothel Interaction: Pay to watch a dance

### Changelog (Original)
7/11/2025 - v1.0.5
* Added 1.6 support

6/1/2024 - v1.0.4
* Fixed a log entry load error
* Added the missing dance spot texture to 1.4 (I am a numpty)

5/18/2024 - v1.0.3
* Now requires Smithing research
* Poles can now be built with wood

5/7/2024 - v1.0.2
* Poles should no longer breakdown and require a component to fix
* Dance Spots are now available in 1.4, but you need to ask sinfulgrailknight for permission to use it. I added it just for them, because they asked so nicely.
