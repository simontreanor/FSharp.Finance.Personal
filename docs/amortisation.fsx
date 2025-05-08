(**
---
title: Amortisation Flowchart
category: Compliance
categoryindex: 4
index: 3
description: Detailed flowchart of amortisation calculations
---

# Amortisation Flowchart

<div class="mermaid">
    flowchart TD
        start([amortise])
        start-->i_par[/parameters/]
        i_par-->bas_par[basic parameters]-->q_sch_con
        i_par-->adv_par[advanced parameters]-->f_tim_pen
        start-->i_act_pay[/actual payments/]
        bas_par-->f_tim_pen
        i_act_pay-->f_tim_pen
        o_bas_sch-->f_tim_pen
        o_app_pay-->f_set_pay_win
        i_par-->f_set_pay_win
        subgraph "calculate basic schedule"
            subgraph "generate items"
                q_sch_con{schedule<br/>config?}
                q_sch_con-->|auto-generated|f_cal_lev_pay
                q_sch_con-->|fixed|o_bas_ite
                q_sch_con-->|custom|o_bas_ite
                f_cal_lev_pay[[calculate level payment]]-->o_bas_ite
                o_bas_ite[/basic items/]
            end
            o_bas_ite-->q_int_met{interest<br/>method?}
            q_int_met-->|add-on|f_cal_ini_int[[calculate initial interest]]-->f_adj_fin_pay
            q_int_met-->|actuarial|f_adj_fin_pay
            f_adj_fin_pay[[adjust final payment]]-->o_bas_sch
            o_bas_sch[/basic schedule/]-->f_cal_apr
            f_cal_apr[[calculate APR]]-->f_cal_sta
            f_cal_sta[[calculate stats]]-->o_bas_sta
            o_bas_sta[/basic stats/]
        end
        o_bas_sta-->f_set_pay_win
        subgraph "apply payments"
            f_tim_pen[[time-out pending payments]]-->f_com_pay
            f_com_pay[[compare scheduled and actual payments]]-->f_gen_set
            f_gen_set[[generate settlement payment]]-->f_app_gro_cha
            f_app_gro_cha[[apply & group charges]]-->o_app_pay
            o_app_pay[/applied payments/]
        end
        subgraph "calculate amortisation"
            f_set_pay_win[[set payment windows]]-->
            f_cal_pay_due[[calculate payment due]]-->
            f_app_cha[[apportion charges]]-->
            f_app_int[[apportion interest]]-->
            f_cal_fee_reb[[calculate fee rebate]]-->
            f_app_fee[[apportion fee]]-->
            f_app_pri[[apportion principal]]-->
            f_car_cha_int[[carry charges & interest]]-->
            f_set_bal_sta[[set balance status]]-->
            f_set_pay_sta[[set payment status]]-->
            f_cal_set_fig[[calculate settlement figure]]-->
            f_cal_gen_pay[[calculate generated payment]]-->
            f_cal_bal[[calculate balances]]-->
            f_mar_mis_pay[[mark missed payments]]-->o_amo_sch
            f_cal_fin_sta[[calculate final stats]]-->o_amo_sta
            o_amo_sch[/amortisation schedule/]-->f_cal_fin_sta
            o_amo_sta[/amortisation stats/]
        end
        o_bas_sch-->finish
        o_bas_sta-->finish
        o_amo_sch-->finish
        o_amo_sta-->finish
        finish([basic schedule & stats,<br/>amortisation&nbsp;schedule&nbsp;&amp;&nbsp;stats])
</div>

*)
