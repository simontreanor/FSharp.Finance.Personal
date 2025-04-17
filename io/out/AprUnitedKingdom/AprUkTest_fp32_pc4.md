<h2>AprUkTest_fp32_pc4</h2>
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
        <td class="ci05">265.25</td>
        <td class="ci06">317.26</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">32</td>
        <td class="ci01" style="white-space: nowrap;">145.63</td>
        <td class="ci02">81.0155</td>
        <td class="ci03">145.63</td>
        <td class="ci04">0.00</td>
        <td class="ci05">119.62</td>
        <td class="ci06">317.26</td>
        <td class="ci07">81.0155</td>
        <td class="ci08">145.63</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">63</td>
        <td class="ci01" style="white-space: nowrap;">145.63</td>
        <td class="ci02">78.4838</td>
        <td class="ci03">119.62</td>
        <td class="ci04">26.01</td>
        <td class="ci05">0.00</td>
        <td class="ci06">291.25</td>
        <td class="ci07">159.4993</td>
        <td class="ci08">265.25</td>
        <td class="ci09">26.01</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">93</td>
        <td class="ci01" style="white-space: nowrap;">145.63</td>
        <td class="ci02">69.7253</td>
        <td class="ci03">0.00</td>
        <td class="ci04">145.63</td>
        <td class="ci05">0.00</td>
        <td class="ci06">145.62</td>
        <td class="ci07">229.2245</td>
        <td class="ci08">265.25</td>
        <td class="ci09">171.64</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">124</td>
        <td class="ci01" style="white-space: nowrap;">145.62</td>
        <td class="ci02">36.0235</td>
        <td class="ci03">0.00</td>
        <td class="ci04">145.62</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">265.2480</td>
        <td class="ci08">265.25</td>
        <td class="ci09">317.26</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>UK APR test amortisation schedule, first payment day 32, payment count 4</i></p>
<p>Generated: <i>2025-04-17 using library version 2.2.0</i></p>
<h4>Parameters</h4>
<table>
    <tr>
        <td>As-of</td>
        <td>2025-04-01</td>
    </tr>
    <tr>
        <td>Start</td>
        <td>2025-04-01</td>
    </tr>
    <tr>
        <td>Principal</td>
        <td>317.26</td>
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
                    <td colspan="2" style="white-space: nowrap;">unit-period config: <i>monthly from 2025-05 on 03</i></td>
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
                    <td>rounding: <i>rounded up</i></td>
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
                    <td>method: <i>add-on</i></td>
                </tr>
                <tr>
                    <td>rounding: <i>rounded down</i></td>
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
                    <td colspan="2">cap: <i>total <i>n/a</i>; daily <i>n/a</i></td>
                </tr>
            </table>
        </td>
    </tr>
</table>
<h4>Initial Stats</h4>
<table>
    <tr>
        <td>Initial interest balance: <i>265.25</i></td>
        <td>Initial cost-to-borrowing ratio: <i>83.61 %</i></td>
        <td>Initial APR: <i>1970.8 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>145.63</i></td>
        <td>Final payment: <i>145.62</i></td>
        <td>Final scheduled payment day: <i>124</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>582.51</i></td>
        <td>Total principal: <i>317.26</i></td>
        <td>Total interest: <i>265.25</i></td>
    </tr>
</table>
