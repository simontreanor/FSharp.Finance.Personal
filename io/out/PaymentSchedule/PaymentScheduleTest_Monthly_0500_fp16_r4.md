<h2>PaymentScheduleTest_Monthly_0500_fp16_r4</h2>
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
        <td class="ci06">500.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">16</td>
        <td class="ci01" style="white-space: nowrap;">190.08</td>
        <td class="ci02">63.8400</td>
        <td class="ci03">63.84</td>
        <td class="ci04">126.24</td>
        <td class="ci05">0.00</td>
        <td class="ci06">373.76</td>
        <td class="ci07">63.8400</td>
        <td class="ci08">63.84</td>
        <td class="ci09">126.24</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">47</td>
        <td class="ci01" style="white-space: nowrap;">190.08</td>
        <td class="ci02">92.4607</td>
        <td class="ci03">92.46</td>
        <td class="ci04">97.62</td>
        <td class="ci05">0.00</td>
        <td class="ci06">276.14</td>
        <td class="ci07">156.3007</td>
        <td class="ci08">156.30</td>
        <td class="ci09">223.86</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">78</td>
        <td class="ci01" style="white-space: nowrap;">190.08</td>
        <td class="ci02">68.3115</td>
        <td class="ci03">68.31</td>
        <td class="ci04">121.77</td>
        <td class="ci05">0.00</td>
        <td class="ci06">154.37</td>
        <td class="ci07">224.6123</td>
        <td class="ci08">224.61</td>
        <td class="ci09">345.63</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">107</td>
        <td class="ci01" style="white-space: nowrap;">190.09</td>
        <td class="ci02">35.7243</td>
        <td class="ci03">35.72</td>
        <td class="ci04">154.37</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">260.3366</td>
        <td class="ci08">260.33</td>
        <td class="ci09">500.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0500 with 16 days to first payment and 4 repayments</i></p>
<p>Generated: <i>2025-04-17 using library version 2.2.0</i></p>
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
        <td>500.00</td>
    </tr>
    <tr>
        <td>Schedule options</td>
        <td>
            <table>
                <tr>
                    <td>config: <i>auto-generate schedule</i></td>
                    <td>schedule length: <i><i>payment count</i> 4</i></td>
                </tr>
                <tr>
                    <td colspan="2" style="white-space: nowrap;">unit-period config: <i>monthly from 2023-12 on 23</i></td>
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
                    <td>level-payment option: <i>higher&nbsp;final&nbsp;payment</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>52.07 %</i></td>
        <td>Initial APR: <i>1309 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>190.08</i></td>
        <td>Final payment: <i>190.09</i></td>
        <td>Final scheduled payment day: <i>107</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>760.33</i></td>
        <td>Total principal: <i>500.00</i></td>
        <td>Total interest: <i>260.33</i></td>
    </tr>
</table>
