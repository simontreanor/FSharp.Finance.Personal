<h2>PaymentScheduleTest_Monthly_0100_fp32_r4</h2>
<table>
    <thead style="vertical-align: bottom;">
        <th style="text-align: right;">Day</th>
        <th style="text-align: right;">Scheduled payment</th>
        <th style="text-align: right;">Actuarial interest</th>
        <th style="text-align: right;">Interest portion</th>
        <th style="text-align: right;">Principal portion</th>
        <th style="text-align: right;">Interest balance</th>
        <th style="text-align: right;">Principal balance</th>
        <th style="text-align: right;">Total actuarial interest</th>
        <th style="text-align: right;">Total interest</th>
        <th style="text-align: right;">Total principal</th>
    </thead>
    <tr style="text-align: right;">
        <td class="ci00">0</td>
        <td class="ci01" style="white-space: nowrap;">0.00</td>
        <td class="ci02">0.0000</td>
        <td class="ci03">0.00</td>
        <td class="ci04">0.00</td>
        <td class="ci05">0.00</td>
        <td class="ci06">100.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">32</td>
        <td class="ci01" style="white-space: nowrap;">42.20</td>
        <td class="ci02">25.5360</td>
        <td class="ci03">25.54</td>
        <td class="ci04">16.66</td>
        <td class="ci05">0.00</td>
        <td class="ci06">83.34</td>
        <td class="ci07">25.5360</td>
        <td class="ci08">25.54</td>
        <td class="ci09">16.66</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">63</td>
        <td class="ci01" style="white-space: nowrap;">42.20</td>
        <td class="ci02">20.6166</td>
        <td class="ci03">20.62</td>
        <td class="ci04">21.58</td>
        <td class="ci05">0.00</td>
        <td class="ci06">61.76</td>
        <td class="ci07">46.1526</td>
        <td class="ci08">46.16</td>
        <td class="ci09">38.24</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">92</td>
        <td class="ci01" style="white-space: nowrap;">42.20</td>
        <td class="ci02">14.2925</td>
        <td class="ci03">14.29</td>
        <td class="ci04">27.91</td>
        <td class="ci05">0.00</td>
        <td class="ci06">33.85</td>
        <td class="ci07">60.4451</td>
        <td class="ci08">60.45</td>
        <td class="ci09">66.15</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">123</td>
        <td class="ci01" style="white-space: nowrap;">42.22</td>
        <td class="ci02">8.3738</td>
        <td class="ci03">8.37</td>
        <td class="ci04">33.85</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">68.8190</td>
        <td class="ci08">68.82</td>
        <td class="ci09">100.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0100 with 32 days to first payment and 4 repayments</i></p>
<p>Generated: <i><a href="../GeneratedDate.html">see details</a></i></p>
<fieldset><legend>Basic Parameters</legend>
<table>
    <tr>
        <td>Evaluation Date</td>
        <td>2023-12-07</td>
    </tr>
    <tr>
        <td>Start Date</td>
        <td>2023-12-07</td>
    </tr>
    <tr>
        <td>Principal</td>
        <td>100.00</td>
    </tr>
    <tr>
        <td>Schedule options</td>
        <td>
            <fieldset>
                <legend>config: <i>auto-generate schedule</i></legend>
                <div>schedule length: <i><i>payment count</i> 4</i></div>
                <div>unit-period config: <i>monthly from 2024-01 on 08</i></div>
            </fieldset>
        </td>
    </tr>
    <tr>
        <td>Payment options</td>
        <td>
            <div>
                <div>rounding: <i>round using AwayFromZero</i></div>
                <div>level-payment option: <i>higher&nbsp;final&nbsp;payment</i></div>
            </div>
        </td>
    </tr>
    <tr>
        <td>Fee options</td>
        <td>no fee
        </td>
    </tr>
    <tr>
        <td>Interest options</td>
        <td>
            <div>
                <div>standard rate: <i>0.798 % per day</i></div>
                <div>method: <i>actuarial</i></div>
                <div>rounding: <i>round using AwayFromZero</i></div>
                <div>APR method: <i>UK FCA</i></div>
                <div>APR precision: <i>1 d.p.</i></div>
                <div>cap: <i>total 100 %; daily 0.8 %</div>
            </div>
        </td>
    </tr>
</table></fieldset>
<fieldset><legend>Initial Stats</legend>
<div>
    <div>Initial interest balance: <i>0.00</i></div>
    <div>Initial cost-to-borrowing ratio: <i>68.82 %</i></div>
    <div>Initial APR: <i>1248.6 %</i></div>
    <div>Level payment: <i>42.20</i></div>
    <div>Final payment: <i>42.22</i></div>
    <div>Last scheduled payment day: <i>123</i></div>
    <div>Total scheduled payments: <i>168.82</i></div>
    <div>Total principal: <i>100.00</i></div>
    <div>Total interest: <i>68.82</i></div>
</div></fieldset>