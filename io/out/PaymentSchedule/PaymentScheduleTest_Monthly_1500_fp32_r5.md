<h2>PaymentScheduleTest_Monthly_1500_fp32_r5</h2>
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
        <td class="ci06">1,500.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">32</td>
        <td class="ci01" style="white-space: nowrap;">554.53</td>
        <td class="ci02">383.0400</td>
        <td class="ci03">383.04</td>
        <td class="ci04">171.49</td>
        <td class="ci05">0.00</td>
        <td class="ci06">1,328.51</td>
        <td class="ci07">383.0400</td>
        <td class="ci08">383.04</td>
        <td class="ci09">171.49</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">63</td>
        <td class="ci01" style="white-space: nowrap;">554.53</td>
        <td class="ci02">328.6468</td>
        <td class="ci03">328.65</td>
        <td class="ci04">225.88</td>
        <td class="ci05">0.00</td>
        <td class="ci06">1,102.63</td>
        <td class="ci07">711.6868</td>
        <td class="ci08">711.69</td>
        <td class="ci09">397.37</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">92</td>
        <td class="ci01" style="white-space: nowrap;">554.53</td>
        <td class="ci02">255.1706</td>
        <td class="ci03">255.17</td>
        <td class="ci04">299.36</td>
        <td class="ci05">0.00</td>
        <td class="ci06">803.27</td>
        <td class="ci07">966.8574</td>
        <td class="ci08">966.86</td>
        <td class="ci09">696.73</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">123</td>
        <td class="ci01" style="white-space: nowrap;">554.53</td>
        <td class="ci02">198.7129</td>
        <td class="ci03">198.71</td>
        <td class="ci04">355.82</td>
        <td class="ci05">0.00</td>
        <td class="ci06">447.45</td>
        <td class="ci07">1,165.5704</td>
        <td class="ci08">1,165.57</td>
        <td class="ci09">1,052.55</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">153</td>
        <td class="ci01" style="white-space: nowrap;">554.57</td>
        <td class="ci02">107.1195</td>
        <td class="ci03">107.12</td>
        <td class="ci04">447.45</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">1,272.6899</td>
        <td class="ci08">1,272.69</td>
        <td class="ci09">1,500.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£1500 with 32 days to first payment and 5 repayments</i></p>
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
        <td>1,500.00</td>
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
    <div>Initial cost-to-borrowing ratio: <i>84.85 %</i></div>
    <div>Initial APR: <i>1249.8 %</i></div>
    <div>Level payment: <i>554.53</i></div>
    <div>Final payment: <i>554.57</i></div>
    <div>Last scheduled payment day: <i>153</i></div>
    <div>Total scheduled payments: <i>2,772.69</i></div>
    <div>Total principal: <i>1,500.00</i></div>
    <div>Total interest: <i>1,272.69</i></div>
</div></fieldset>