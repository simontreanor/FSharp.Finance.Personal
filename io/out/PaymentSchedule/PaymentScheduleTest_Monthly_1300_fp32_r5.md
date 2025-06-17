<h2>PaymentScheduleTest_Monthly_1300_fp32_r5</h2>
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
        <td class="ci06">1,300.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">32</td>
        <td class="ci01" style="white-space: nowrap;">480.60</td>
        <td class="ci02">331.9680</td>
        <td class="ci03">331.97</td>
        <td class="ci04">148.63</td>
        <td class="ci05">0.00</td>
        <td class="ci06">1,151.37</td>
        <td class="ci07">331.9680</td>
        <td class="ci08">331.97</td>
        <td class="ci09">148.63</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">63</td>
        <td class="ci01" style="white-space: nowrap;">480.60</td>
        <td class="ci02">284.8259</td>
        <td class="ci03">284.83</td>
        <td class="ci04">195.77</td>
        <td class="ci05">0.00</td>
        <td class="ci06">955.60</td>
        <td class="ci07">616.7939</td>
        <td class="ci08">616.80</td>
        <td class="ci09">344.40</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">92</td>
        <td class="ci01" style="white-space: nowrap;">480.60</td>
        <td class="ci02">221.1450</td>
        <td class="ci03">221.14</td>
        <td class="ci04">259.46</td>
        <td class="ci05">0.00</td>
        <td class="ci06">696.14</td>
        <td class="ci07">837.9389</td>
        <td class="ci08">837.94</td>
        <td class="ci09">603.86</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">123</td>
        <td class="ci01" style="white-space: nowrap;">480.60</td>
        <td class="ci02">172.2111</td>
        <td class="ci03">172.21</td>
        <td class="ci04">308.39</td>
        <td class="ci05">0.00</td>
        <td class="ci06">387.75</td>
        <td class="ci07">1,010.1500</td>
        <td class="ci08">1,010.15</td>
        <td class="ci09">912.25</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">153</td>
        <td class="ci01" style="white-space: nowrap;">480.58</td>
        <td class="ci02">92.8274</td>
        <td class="ci03">92.83</td>
        <td class="ci04">387.75</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">1,102.9773</td>
        <td class="ci08">1,102.98</td>
        <td class="ci09">1,300.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£1300 with 32 days to first payment and 5 repayments</i></p>
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
        <td>1,300.00</td>
    </tr>
    <tr>
        <td>Schedule options</td>
        <td>
            <fieldset>
                <legend>config: <i>auto-generate schedule</i></legend>
                <div>schedule length: <i><i>payment count</i> 5</i></div>
                <div>unit-period config: <i>monthly from 2024-01 on 08</i></div>
            </fieldset>
        </td>
    </tr>
    <tr>
        <td>Payment options</td>
        <td>
            <div>
                <div>rounding: <i>round using AwayFromZero</i></div>
                <div>level-payment option: <i>lower&nbsp;final&nbsp;payment</i></div>
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
    <div>Initial cost-to-borrowing ratio: <i>84.84 %</i></div>
    <div>Initial APR: <i>1249.8 %</i></div>
    <div>Level payment: <i>480.60</i></div>
    <div>Final payment: <i>480.58</i></div>
    <div>Last scheduled payment day: <i>153</i></div>
    <div>Total scheduled payments: <i>2,402.98</i></div>
    <div>Total principal: <i>1,300.00</i></div>
    <div>Total interest: <i>1,102.98</i></div>
</div></fieldset>