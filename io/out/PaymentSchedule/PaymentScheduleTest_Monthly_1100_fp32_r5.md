<h2>PaymentScheduleTest_Monthly_1100_fp32_r5</h2>
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
        <td class="ci06">1,100.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">32</td>
        <td class="ci01" style="white-space: nowrap;">406.66</td>
        <td class="ci02">280.8960</td>
        <td class="ci03">280.90</td>
        <td class="ci04">125.76</td>
        <td class="ci05">0.00</td>
        <td class="ci06">974.24</td>
        <td class="ci07">280.8960</td>
        <td class="ci08">280.90</td>
        <td class="ci09">125.76</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">63</td>
        <td class="ci01" style="white-space: nowrap;">406.66</td>
        <td class="ci02">241.0075</td>
        <td class="ci03">241.01</td>
        <td class="ci04">165.65</td>
        <td class="ci05">0.00</td>
        <td class="ci06">808.59</td>
        <td class="ci07">521.9035</td>
        <td class="ci08">521.91</td>
        <td class="ci09">291.41</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">92</td>
        <td class="ci01" style="white-space: nowrap;">406.66</td>
        <td class="ci02">187.1239</td>
        <td class="ci03">187.12</td>
        <td class="ci04">219.54</td>
        <td class="ci05">0.00</td>
        <td class="ci06">589.05</td>
        <td class="ci07">709.0274</td>
        <td class="ci08">709.03</td>
        <td class="ci09">510.95</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">123</td>
        <td class="ci01" style="white-space: nowrap;">406.66</td>
        <td class="ci02">145.7192</td>
        <td class="ci03">145.72</td>
        <td class="ci04">260.94</td>
        <td class="ci05">0.00</td>
        <td class="ci06">328.11</td>
        <td class="ci07">854.7466</td>
        <td class="ci08">854.75</td>
        <td class="ci09">771.89</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">153</td>
        <td class="ci01" style="white-space: nowrap;">406.66</td>
        <td class="ci02">78.5495</td>
        <td class="ci03">78.55</td>
        <td class="ci04">328.11</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">933.2961</td>
        <td class="ci08">933.30</td>
        <td class="ci09">1,100.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£1100 with 32 days to first payment and 5 repayments</i></p>
<p>Generated: <i>2025-04-28 using library version 2.2.10</i></p>
<h4>Parameters</h4>
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
        <td>1,100.00</td>
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
        <td>Initial cost-to-borrowing ratio: <i>84.85 %</i></td>
        <td>Initial APR: <i>1249.8 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>406.66</i></td>
        <td>Final payment: <i>406.66</i></td>
        <td>Last scheduled payment day: <i>153</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>2,033.30</i></td>
        <td>Total principal: <i>1,100.00</i></td>
        <td>Total interest: <i>933.30</i></td>
    </tr>
</table>
