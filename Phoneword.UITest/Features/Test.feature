Feature: Test
	Test how to define scenario and steps to be used on both Android and iPhone.

@mytag
Scenario: Translate Text And Dial
	Given I have launched Phoneword app successfully
	And I have entered phone word of "13 MeetUp"
	When I press Translate button
	Then I can see the phone number from the phone text
	When I tap the call button
	And I tap the NO button
	Then I can see the popup is dismissed