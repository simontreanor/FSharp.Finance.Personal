<h2>PaymentScheduleTest_Monthly_0900_fp32_r5</h2>
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
        <td class="ci06">900.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">32</td>
        <td class="ci01" style="white-space: nowrap;">332.72</td>
        <td class="ci02">229.8240</td>
        <td class="ci03">229.82</td>
        <td class="ci04">102.90</td>
        <td class="ci05">0.00</td>
        <td class="ci06">797.10</td>
        <td class="ci07">229.8240</td>
        <td class="ci08">229.82</td>
        <td class="ci09">102.90</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">63</td>
        <td class="ci01" style="white-space: nowrap;">332.72</td>
        <td class="ci02">197.1866</td>
        <td class="ci03">197.19</td>
        <td class="ci04">135.53</td>
        <td class="ci05">0.00</td>
        <td class="ci06">661.57</td>
        <td class="ci07">427.0106</td>
        <td class="ci08">427.01</td>
        <td class="ci09">238.43</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">92</td>
        <td class="ci01" style="white-space: nowrap;">332.72</td>
        <td class="ci02">153.1005</td>
        <td class="ci03">153.10</td>
        <td class="ci04">179.62</td>
        <td class="ci05">0.00</td>
        <td class="ci06">481.95</td>
        <td class="ci07">580.1111</td>
        <td class="ci08">580.11</td>
        <td class="ci09">418.05</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">123</td>
        <td class="ci01" style="white-space: nowrap;">332.72</td>
        <td class="ci02">119.2248</td>
        <td class="ci03">119.22</td>
        <td class="ci04">213.50</td>
        <td class="ci05">0.00</td>
        <td class="ci06">268.45</td>
        <td class="ci07">699.3359</td>
        <td class="ci08">699.33</td>
        <td class="ci09">631.55</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">153</td>
        <td class="ci01" style="white-space: nowrap;">332.72</td>
        <td class="ci02">64.2669</td>
        <td class="ci03">64.27</td>
        <td class="ci04">268.45</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">763.6028</td>
        <td class="ci08">763.60</td>
        <td class="ci09">900.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0900 with 32 days to first payment and 5 repayments</i></p>
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
        <td>900.00</td>
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
    <div>Level payment: <i>332.72</i></div>
    <div>Final payment: <i>332.72</i></div>
    <div>Last scheduled payment day: <i>153</i></div>
    <div>Total scheduled payments: <i>1,663.60</i></div>
    <div>Total principal: <i>900.00</i></div>
    <div>Total interest: <i>763.60</i></div>
</div></fieldset>