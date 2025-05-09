<h2>PaymentScheduleTest_Monthly_0500_fp12_r4</h2>
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
        <td class="ci00">12</td>
        <td class="ci01" style="white-space: nowrap;">184.70</td>
        <td class="ci02">47.8800</td>
        <td class="ci03">47.88</td>
        <td class="ci04">136.82</td>
        <td class="ci05">0.00</td>
        <td class="ci06">363.18</td>
        <td class="ci07">47.8800</td>
        <td class="ci08">47.88</td>
        <td class="ci09">136.82</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">43</td>
        <td class="ci01" style="white-space: nowrap;">184.70</td>
        <td class="ci02">89.8435</td>
        <td class="ci03">89.84</td>
        <td class="ci04">94.86</td>
        <td class="ci05">0.00</td>
        <td class="ci06">268.32</td>
        <td class="ci07">137.7235</td>
        <td class="ci08">137.72</td>
        <td class="ci09">231.68</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">74</td>
        <td class="ci01" style="white-space: nowrap;">184.70</td>
        <td class="ci02">66.3770</td>
        <td class="ci03">66.38</td>
        <td class="ci04">118.32</td>
        <td class="ci05">0.00</td>
        <td class="ci06">150.00</td>
        <td class="ci07">204.1005</td>
        <td class="ci08">204.10</td>
        <td class="ci09">350.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">103</td>
        <td class="ci01" style="white-space: nowrap;">184.71</td>
        <td class="ci02">34.7130</td>
        <td class="ci03">34.71</td>
        <td class="ci04">150.00</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">238.8135</td>
        <td class="ci08">238.81</td>
        <td class="ci09">500.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0500 with 12 days to first payment and 4 repayments</i></p>
<p>Generated: <i>2025-05-09 using library version 2.4.5</i></p>
<h4>Basic Parameters</h4>
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
            <table>
                <tr>
                    <td>config: <i>auto-generate schedule</i></td>
                    <td>schedule length: <i><i>payment count</i> 4</i></td>
                </tr>
                <tr>
                    <td colspan="2" style="white-space: nowrap;">unit-period config: <i>monthly from 2023-12 on 19</i></td>
                </tr>
            </table>
        </td>
    </tr>
    <tr>
        <td>Payment options</td>
        <td>
            <table>
                <tr>
                    <td>rounding: <i>round using AwayFromZero</i></td>
                </tr>
                <tr>
                    <td>level-payment option: <i>similar&nbsp;final&nbsp;payment</i></td>
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
        <td>Interest options</td>
        <td>
            <table>
                <tr>
                    <td>standard rate: <i>0.798 % per day</i></td>
                    <td>method: <i>actuarial</i></td>
                </tr>
                <tr>
                    <td>rounding: <i>round using AwayFromZero</i></td>
                    <td>APR method: <i>UK FCA to 1 d.p.</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>47.76 %</i></td>
        <td>Initial APR: <i>1311.9 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>184.70</i></td>
        <td>Final payment: <i>184.71</i></td>
        <td>Last scheduled payment day: <i>103</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>738.81</i></td>
        <td>Total principal: <i>500.00</i></td>
        <td>Total interest: <i>238.81</i></td>
    </tr>
</table>