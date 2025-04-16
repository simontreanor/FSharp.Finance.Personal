<h2>PaymentScheduleTest002</h2>
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
        <td class="ci06">1,000.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">0</td>
        <td class="ci01" style="white-space: nowrap;">269.20</td>
        <td class="ci02">0.0000</td>
        <td class="ci03">0.00</td>
        <td class="ci04">269.20</td>
        <td class="ci05">0.00</td>
        <td class="ci06">730.80</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">269.20</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">31</td>
        <td class="ci01" style="white-space: nowrap;">269.20</td>
        <td class="ci02">181.2384</td>
        <td class="ci03">181.23</td>
        <td class="ci04">87.97</td>
        <td class="ci05">0.00</td>
        <td class="ci06">642.83</td>
        <td class="ci07">181.2384</td>
        <td class="ci08">181.23</td>
        <td class="ci09">357.17</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">61</td>
        <td class="ci01" style="white-space: nowrap;">269.20</td>
        <td class="ci02">154.2792</td>
        <td class="ci03">154.27</td>
        <td class="ci04">114.93</td>
        <td class="ci05">0.00</td>
        <td class="ci06">527.90</td>
        <td class="ci07">335.5176</td>
        <td class="ci08">335.50</td>
        <td class="ci09">472.10</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">92</td>
        <td class="ci01" style="white-space: nowrap;">269.20</td>
        <td class="ci02">130.9192</td>
        <td class="ci03">130.91</td>
        <td class="ci04">138.29</td>
        <td class="ci05">0.00</td>
        <td class="ci06">389.61</td>
        <td class="ci07">466.4368</td>
        <td class="ci08">466.41</td>
        <td class="ci09">610.39</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">123</td>
        <td class="ci01" style="white-space: nowrap;">269.20</td>
        <td class="ci02">96.6233</td>
        <td class="ci03">96.62</td>
        <td class="ci04">172.58</td>
        <td class="ci05">0.00</td>
        <td class="ci06">217.03</td>
        <td class="ci07">563.0601</td>
        <td class="ci08">563.03</td>
        <td class="ci09">782.97</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">153</td>
        <td class="ci01" style="white-space: nowrap;">269.11</td>
        <td class="ci02">52.0872</td>
        <td class="ci03">52.08</td>
        <td class="ci04">217.03</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">615.1473</td>
        <td class="ci08">615.11</td>
        <td class="ci09">1,000.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>Term must not exceed maximum duration</i></p>
<p>Generated: <i>2025-04-16 using library version 2.1.2</i></p>
<h4>Parameters</h4>
<table>
    <tr>
        <td>As-of</td>
        <td>2024-05-08</td>
    </tr>
    <tr>
        <td>Start</td>
        <td>2024-05-08</td>
    </tr>
    <tr>
        <td>Principal</td>
        <td>1,000.00</td>
    </tr>
    <tr>
        <td>Schedule options</td>
        <td>
            <table>
                <tr>
                    <td>config: <i>auto-generate schedule</i></td>
                    <td>payment count: <i>7</i></td>
                </tr>
                <tr>
                    <td style="white-space: nowrap;">unit-period config: <i>monthly from 2024-05 on 08</i></td>
                    <td>max duration: <i>maximum 183 days from 2024-05-08</i></td>
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
                    <td>balance-close: <i>leave&nbsp;open&nbsp;balance</i></td>
                </tr>
                <tr>
                    <td>rounding: <i>rounded up</i></td>
                    <td>timeout: <i>3</i></td>
                </tr>
                <tr>
                    <td colspan='2'>minimum: <i>defer&nbsp;or&nbsp;write&nbsp;off&nbsp;up&nbsp;to&nbsp;0.50</i></td>
                </tr>
                <tr>
                    <td colspan='2'>level-payment option: <i>lower&nbsp;final&nbsp;payment</i></td>
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
        <td>
            <table>
                <tr>
                    <th>Type</th>
                    <th>Charge</th>
                    <th>Grouping</th>
                    <th>Holidays</th>
                </tr>
                <tr>
                    <td>late payment</td>
                    <td>7.50</td><td>one charge per day</td><td><i>n/a</i></td>
                </tr>
            </table>
        </td>
    </tr>
    <tr>
        <td>Interest options</td>
        <td>
            <table>
                <tr>
                    <td>standard rate: <i>0.8 % per day</i></td>
                    <td>method: <i>simple</i></td>
                </tr>
                <tr>
                    <td>rounding: <i>rounded down</i></td>
                    <td>APR method: <i>UK FCA to 1 d.p.</i></td>
                </tr>
                <tr>
                    <td>initial grace period: <i>0 day(s)</i></td>
                    <td>rate on negative balance: <i>8 % per year</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>61.51 %</i></td>
        <td>Initial APR: <i>1261.4 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>269.20</i></td>
        <td>Final payment: <i>269.11</i></td>
        <td>Final scheduled payment day: <i>153</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>1,615.11</i></td>
        <td>Total principal: <i>1,000.00</i></td>
        <td>Total interest: <i>615.11</i></td>
    </tr>
</table>
