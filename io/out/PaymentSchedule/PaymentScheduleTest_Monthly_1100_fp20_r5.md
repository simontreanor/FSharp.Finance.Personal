<h2>PaymentScheduleTest_Monthly_1100_fp20_r5</h2>
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
        <td class="ci00">20</td>
        <td class="ci01" style="white-space: nowrap;">376.87</td>
        <td class="ci02">175.5600</td>
        <td class="ci03">175.56</td>
        <td class="ci04">201.31</td>
        <td class="ci05">0.00</td>
        <td class="ci06">898.69</td>
        <td class="ci07">175.5600</td>
        <td class="ci08">175.56</td>
        <td class="ci09">201.31</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">51</td>
        <td class="ci01" style="white-space: nowrap;">376.87</td>
        <td class="ci02">222.3179</td>
        <td class="ci03">222.32</td>
        <td class="ci04">154.55</td>
        <td class="ci05">0.00</td>
        <td class="ci06">744.14</td>
        <td class="ci07">397.8779</td>
        <td class="ci08">397.88</td>
        <td class="ci09">355.86</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">82</td>
        <td class="ci01" style="white-space: nowrap;">376.87</td>
        <td class="ci02">184.0854</td>
        <td class="ci03">184.09</td>
        <td class="ci04">192.78</td>
        <td class="ci05">0.00</td>
        <td class="ci06">551.36</td>
        <td class="ci07">581.9633</td>
        <td class="ci08">581.97</td>
        <td class="ci09">548.64</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">111</td>
        <td class="ci01" style="white-space: nowrap;">376.87</td>
        <td class="ci02">127.5957</td>
        <td class="ci03">127.60</td>
        <td class="ci04">249.27</td>
        <td class="ci05">0.00</td>
        <td class="ci06">302.09</td>
        <td class="ci07">709.5590</td>
        <td class="ci08">709.57</td>
        <td class="ci09">797.91</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">142</td>
        <td class="ci01" style="white-space: nowrap;">376.82</td>
        <td class="ci02">74.7310</td>
        <td class="ci03">74.73</td>
        <td class="ci04">302.09</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">784.2900</td>
        <td class="ci08">784.30</td>
        <td class="ci09">1,100.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£1100 with 20 days to first payment and 5 repayments</i></p>
<p>Generated: <i>2025-04-15 at 20:41:49</i></p>
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
        <td>1,100.00</td>
    </tr>
    <tr>
        <td>Schedule options</td>
        <td>
            <table>
                <tr>
                    <td>config: <i>auto-generate schedule</i></td>
                    <td>payment count: <i>5</i></td>
                </tr>
                <tr>
                    <td style="white-space: nowrap;">unit-period config: <i>monthly from 2023-12 on 27</i></td>
                    <td>max duration: <i>unlimited</i></td>
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
                    <td>rounding: <i>round using AwayFromZero</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>71.3 %</i></td>
        <td>Initial APR: <i>1292.9 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>376.87</i></td>
        <td>Final payment: <i>376.82</i></td>
        <td>Final scheduled payment day: <i>142</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>1,884.30</i></td>
        <td>Total principal: <i>1,100.00</i></td>
        <td>Total interest: <i>784.30</i></td>
    </tr>
</table>
