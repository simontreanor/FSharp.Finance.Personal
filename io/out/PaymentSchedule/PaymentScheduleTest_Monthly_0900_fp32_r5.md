<h2>PaymentScheduleTest_Monthly_0900_fp32_r5</h2>
<table>
    <thead style="vertical-align: bottom;">
        <th style="text-align: right;">Day</th>
        <th style="text-align: right;">Scheduled payment</th>
        <th style="text-align: right;">Simple interest</th>
        <th style="text-align: right;">Interest portion</th>
        <th style="text-align: right;">Principal portion</th>
        <th style="text-align: right;">Interest balance</th>
        <th style="text-align: right;">Principal balance</th>
        <th style="text-align: right;">Total simple interest</th>
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
<p>Generated: <i>2025-04-23 using library version 2.2.4</i></p>
<h4>Parameters</h4>
<table>
    <tr>
        <td>As-of</td>
        <td>2023-12-07</td>
    </tr>
    <tr>
        <td>Start</td>
        <td>2023-12-07</td>
    </tr>
    <tr>
        <td>Principal</td>
        <td>900.00</td>
    </tr>
    <tr>
        <td>Schedule options</td>
        <td>
            <table>
                <tr>
                    <td>config: <i>auto-generate schedule</i></td>
                    <td>schedule length: <i><i>payment count</i> 5</i></td>
                </tr>
                <tr>
                    <td colspan="2" style="white-space: nowrap;">unit-period config: <i>monthly from 2024-01 on 08</i></td>
                </tr>
            </table>
        </td>
    </tr>
    <tr>
        <td>Payment options</td>
        <td>
            <table>
                <tr>
                    <td>scheduling: <i>as scheduled</i></td>
                </tr>
                <tr>
                    <td>rounding: <i>round using AwayFromZero</i></td>
                </tr>
                <tr>
                    <td>timeout: <i>3</i></td>
                </tr>
                <tr>
                    <td>minimum: <i>defer&nbsp;or&nbsp;write&nbsp;off&nbsp;up&nbsp;to&nbsp;0.50</i></td>
                </tr>
                <tr>
                    <td>level-payment option: <i>lower&nbsp;final&nbsp;payment</i></td>
                </tr>
            </table>
        </td>
    </tr>
    <tr>
        <td>Fee options</td>
        <td>no fee
        </td>
    </tr>
    <tr>
        <td>Charge options</td>
        <td>no charges
        </td>
    </tr>
    <tr>
        <td>Interest options</td>
        <td>
            <table>
                <tr>
                    <td>standard rate: <i>0.798 % per day</i></td>
                    <td>method: <i>simple</i></td>
                </tr>
                <tr>
                    <td>rounding: <i>round using AwayFromZero</i></td>
                    <td>APR method: <i>UK FCA to 1 d.p.</i></td>
                </tr>
                <tr>
                    <td>initial grace period: <i>3 day(s)</i></td>
                    <td>rate on negative balance: <i>zero</i></td>
                </tr>
                <tr>
                    <td colspan="2">promotional rates: <i><i>n/a</i></i></td>
                </tr>
                <tr>
                    <td colspan="2">cap: <i>total 100 %; daily 0.8 %</td>
                </tr>
            </table>
        </td>
    </tr>
</table>
<h4>Initial Stats</h4>
<table>
    <tr>
        <td>Initial interest balance: <i>0.00</i></td>
        <td>Initial cost-to-borrowing ratio: <i>84.84 %</i></td>
        <td>Initial APR: <i>1249.8 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>332.72</i></td>
        <td>Final payment: <i>332.72</i></td>
        <td>Last scheduled payment day: <i>153</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>1,663.60</i></td>
        <td>Total principal: <i>900.00</i></td>
        <td>Total interest: <i>763.60</i></td>
    </tr>
</table>
