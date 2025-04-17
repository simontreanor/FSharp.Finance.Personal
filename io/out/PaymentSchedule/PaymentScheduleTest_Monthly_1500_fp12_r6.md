<h2>PaymentScheduleTest_Monthly_1500_fp12_r6</h2>
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
        <td class="ci06">1,500.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">12</td>
        <td class="ci01" style="white-space: nowrap;">441.57</td>
        <td class="ci02">143.6400</td>
        <td class="ci03">143.64</td>
        <td class="ci04">297.93</td>
        <td class="ci05">0.00</td>
        <td class="ci06">1,202.07</td>
        <td class="ci07">143.6400</td>
        <td class="ci08">143.64</td>
        <td class="ci09">297.93</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">43</td>
        <td class="ci01" style="white-space: nowrap;">441.57</td>
        <td class="ci02">297.3681</td>
        <td class="ci03">297.37</td>
        <td class="ci04">144.20</td>
        <td class="ci05">0.00</td>
        <td class="ci06">1,057.87</td>
        <td class="ci07">441.0081</td>
        <td class="ci08">441.01</td>
        <td class="ci09">442.13</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">74</td>
        <td class="ci01" style="white-space: nowrap;">441.57</td>
        <td class="ci02">261.6959</td>
        <td class="ci03">261.70</td>
        <td class="ci04">179.87</td>
        <td class="ci05">0.00</td>
        <td class="ci06">878.00</td>
        <td class="ci07">702.7040</td>
        <td class="ci08">702.71</td>
        <td class="ci09">622.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">103</td>
        <td class="ci01" style="white-space: nowrap;">441.57</td>
        <td class="ci02">203.1868</td>
        <td class="ci03">203.19</td>
        <td class="ci04">238.38</td>
        <td class="ci05">0.00</td>
        <td class="ci06">639.62</td>
        <td class="ci07">905.8907</td>
        <td class="ci08">905.90</td>
        <td class="ci09">860.38</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">134</td>
        <td class="ci01" style="white-space: nowrap;">441.57</td>
        <td class="ci02">158.2292</td>
        <td class="ci03">158.23</td>
        <td class="ci04">283.34</td>
        <td class="ci05">0.00</td>
        <td class="ci06">356.28</td>
        <td class="ci07">1,064.1199</td>
        <td class="ci08">1,064.13</td>
        <td class="ci09">1,143.72</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">164</td>
        <td class="ci01" style="white-space: nowrap;">441.57</td>
        <td class="ci02">85.2934</td>
        <td class="ci03">85.29</td>
        <td class="ci04">356.28</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">1,149.4133</td>
        <td class="ci08">1,149.42</td>
        <td class="ci09">1,500.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£1500 with 12 days to first payment and 6 repayments</i></p>
<p>Generated: <i>2025-04-17 using library version 2.1.2</i></p>
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
        <td>1,500.00</td>
    </tr>
    <tr>
        <td>Schedule options</td>
        <td>
            <table>
                <tr>
                    <td>config: <i>auto-generate schedule</i></td>
                    <td>schedule length: <i><i>payment count</i> 6</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>76.63 %</i></td>
        <td>Initial APR: <i>1296.2 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>441.57</i></td>
        <td>Final payment: <i>441.57</i></td>
        <td>Final scheduled payment day: <i>164</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>2,649.42</i></td>
        <td>Total principal: <i>1,500.00</i></td>
        <td>Total interest: <i>1,149.42</i></td>
    </tr>
</table>
