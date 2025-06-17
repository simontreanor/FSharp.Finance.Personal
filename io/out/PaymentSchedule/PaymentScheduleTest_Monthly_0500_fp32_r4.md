<h2>PaymentScheduleTest_Monthly_0500_fp32_r4</h2>
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
        <td class="ci06">500.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">32</td>
        <td class="ci01" style="white-space: nowrap;">211.01</td>
        <td class="ci02">127.6800</td>
        <td class="ci03">127.68</td>
        <td class="ci04">83.33</td>
        <td class="ci05">0.00</td>
        <td class="ci06">416.67</td>
        <td class="ci07">127.6800</td>
        <td class="ci08">127.68</td>
        <td class="ci09">83.33</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">63</td>
        <td class="ci01" style="white-space: nowrap;">211.01</td>
        <td class="ci02">103.0758</td>
        <td class="ci03">103.08</td>
        <td class="ci04">107.93</td>
        <td class="ci05">0.00</td>
        <td class="ci06">308.74</td>
        <td class="ci07">230.7558</td>
        <td class="ci08">230.76</td>
        <td class="ci09">191.26</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">92</td>
        <td class="ci01" style="white-space: nowrap;">211.01</td>
        <td class="ci02">71.4486</td>
        <td class="ci03">71.45</td>
        <td class="ci04">139.56</td>
        <td class="ci05">0.00</td>
        <td class="ci06">169.18</td>
        <td class="ci07">302.2044</td>
        <td class="ci08">302.21</td>
        <td class="ci09">330.82</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">123</td>
        <td class="ci01" style="white-space: nowrap;">211.03</td>
        <td class="ci02">41.8517</td>
        <td class="ci03">41.85</td>
        <td class="ci04">169.18</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">344.0562</td>
        <td class="ci08">344.06</td>
        <td class="ci09">500.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0500 with 32 days to first payment and 4 repayments</i></p>
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
        <td>500.00</td>
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
                <div>level-payment option: <i>similar&nbsp;final&nbsp;payment</i></div>
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
    <div>Initial cost-to-borrowing ratio: <i>68.81 %</i></div>
    <div>Initial APR: <i>1248.5 %</i></div>
    <div>Level payment: <i>211.01</i></div>
    <div>Final payment: <i>211.03</i></div>
    <div>Last scheduled payment day: <i>123</i></div>
    <div>Total scheduled payments: <i>844.06</i></div>
    <div>Total principal: <i>500.00</i></div>
    <div>Total interest: <i>344.06</i></div>
</div></fieldset>