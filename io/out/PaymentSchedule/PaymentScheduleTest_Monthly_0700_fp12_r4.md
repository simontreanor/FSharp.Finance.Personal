<h2>PaymentScheduleTest_Monthly_0700_fp12_r4</h2>
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
        <td class="ci06">700.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">12</td>
        <td class="ci01" style="white-space: nowrap;">258.59</td>
        <td class="ci02">67.0320</td>
        <td class="ci03">67.03</td>
        <td class="ci04">191.56</td>
        <td class="ci05">0.00</td>
        <td class="ci06">508.44</td>
        <td class="ci07">67.0320</td>
        <td class="ci08">67.03</td>
        <td class="ci09">191.56</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">43</td>
        <td class="ci01" style="white-space: nowrap;">258.59</td>
        <td class="ci02">125.7779</td>
        <td class="ci03">125.78</td>
        <td class="ci04">132.81</td>
        <td class="ci05">0.00</td>
        <td class="ci06">375.63</td>
        <td class="ci07">192.8099</td>
        <td class="ci08">192.81</td>
        <td class="ci09">324.37</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">74</td>
        <td class="ci01" style="white-space: nowrap;">258.59</td>
        <td class="ci02">92.9233</td>
        <td class="ci03">92.92</td>
        <td class="ci04">165.67</td>
        <td class="ci05">0.00</td>
        <td class="ci06">209.96</td>
        <td class="ci07">285.7332</td>
        <td class="ci08">285.73</td>
        <td class="ci09">490.04</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">103</td>
        <td class="ci01" style="white-space: nowrap;">258.55</td>
        <td class="ci02">48.5889</td>
        <td class="ci03">48.59</td>
        <td class="ci04">209.96</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">334.3222</td>
        <td class="ci08">334.32</td>
        <td class="ci09">700.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0700 with 12 days to first payment and 4 repayments</i></p>
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
        <td>Initial cost-to-borrowing ratio: <i>47.76 %</i></td>
        <td>Initial APR: <i>1311.9 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>258.59</i></td>
        <td>Final payment: <i>258.55</i></td>
        <td>Last scheduled payment day: <i>103</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>1,034.32</i></td>
        <td>Total principal: <i>700.00</i></td>
        <td>Total interest: <i>334.32</i></td>
    </tr>
</table>
