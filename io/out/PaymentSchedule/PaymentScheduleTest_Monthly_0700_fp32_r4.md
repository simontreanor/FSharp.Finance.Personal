<h2>PaymentScheduleTest_Monthly_0700_fp32_r4</h2>
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
        <td class="ci06">700.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">32</td>
        <td class="ci01" style="white-space: nowrap;">295.42</td>
        <td class="ci02">178.7520</td>
        <td class="ci03">178.75</td>
        <td class="ci04">116.67</td>
        <td class="ci05">0.00</td>
        <td class="ci06">583.33</td>
        <td class="ci07">178.7520</td>
        <td class="ci08">178.75</td>
        <td class="ci09">116.67</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">63</td>
        <td class="ci01" style="white-space: nowrap;">295.42</td>
        <td class="ci02">144.3042</td>
        <td class="ci03">144.30</td>
        <td class="ci04">151.12</td>
        <td class="ci05">0.00</td>
        <td class="ci06">432.21</td>
        <td class="ci07">323.0562</td>
        <td class="ci08">323.05</td>
        <td class="ci09">267.79</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">92</td>
        <td class="ci01" style="white-space: nowrap;">295.42</td>
        <td class="ci02">100.0220</td>
        <td class="ci03">100.02</td>
        <td class="ci04">195.40</td>
        <td class="ci05">0.00</td>
        <td class="ci06">236.81</td>
        <td class="ci07">423.0782</td>
        <td class="ci08">423.07</td>
        <td class="ci09">463.19</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">123</td>
        <td class="ci01" style="white-space: nowrap;">295.39</td>
        <td class="ci02">58.5821</td>
        <td class="ci03">58.58</td>
        <td class="ci04">236.81</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">481.6603</td>
        <td class="ci08">481.65</td>
        <td class="ci09">700.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0700 with 32 days to first payment and 4 repayments</i></p>
<p>Generated: <i><a href="../GeneratedDate.md">see details</a></i></p>
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
        <td>700.00</td>
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
                    <td>rounding: <i>round using AwayFromZero</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>68.81 %</i></td>
        <td>Initial APR: <i>1248.4 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>295.42</i></td>
        <td>Final payment: <i>295.39</i></td>
        <td>Last scheduled payment day: <i>123</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>1,181.65</i></td>
        <td>Total principal: <i>700.00</i></td>
        <td>Total interest: <i>481.65</i></td>
    </tr>
</table>