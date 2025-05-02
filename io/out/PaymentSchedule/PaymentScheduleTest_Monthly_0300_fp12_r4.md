<h2>PaymentScheduleTest_Monthly_0300_fp12_r4</h2>
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
        <td class="ci06">300.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">12</td>
        <td class="ci01" style="white-space: nowrap;">110.82</td>
        <td class="ci02">28.7280</td>
        <td class="ci03">28.73</td>
        <td class="ci04">82.09</td>
        <td class="ci05">0.00</td>
        <td class="ci06">217.91</td>
        <td class="ci07">28.7280</td>
        <td class="ci08">28.73</td>
        <td class="ci09">82.09</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">43</td>
        <td class="ci01" style="white-space: nowrap;">110.82</td>
        <td class="ci02">53.9066</td>
        <td class="ci03">53.91</td>
        <td class="ci04">56.91</td>
        <td class="ci05">0.00</td>
        <td class="ci06">161.00</td>
        <td class="ci07">82.6346</td>
        <td class="ci08">82.64</td>
        <td class="ci09">139.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">74</td>
        <td class="ci01" style="white-space: nowrap;">110.82</td>
        <td class="ci02">39.8282</td>
        <td class="ci03">39.83</td>
        <td class="ci04">70.99</td>
        <td class="ci05">0.00</td>
        <td class="ci06">90.01</td>
        <td class="ci07">122.4628</td>
        <td class="ci08">122.47</td>
        <td class="ci09">209.99</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">103</td>
        <td class="ci01" style="white-space: nowrap;">110.84</td>
        <td class="ci02">20.8301</td>
        <td class="ci03">20.83</td>
        <td class="ci04">90.01</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">143.2929</td>
        <td class="ci08">143.30</td>
        <td class="ci09">300.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0300 with 12 days to first payment and 4 repayments</i></p>
<p>Generated: <i>2025-05-02 using library version 2.3.1</i></p>
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
        <td>300.00</td>
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
                    <td>method: <i>simple</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>47.77 %</i></td>
        <td>Initial APR: <i>1312.1 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>110.82</i></td>
        <td>Final payment: <i>110.84</i></td>
        <td>Last scheduled payment day: <i>103</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>443.30</i></td>
        <td>Total principal: <i>300.00</i></td>
        <td>Total interest: <i>143.30</i></td>
    </tr>
</table>