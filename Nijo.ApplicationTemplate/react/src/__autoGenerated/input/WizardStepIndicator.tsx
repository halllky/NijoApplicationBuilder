
/**
 * ウィザード画面の、いま表示されているのが全体の何ステップ目なのかを表す表示
 */
export const WizardStepIndicator = ({ currentStep, allSteps, onClickStep, className }: {
  currentStep: number
  allSteps: number[]
  onClickStep?: (step: number) => void
  className?: string
}) => {
  const currentStepIndex = allSteps.indexOf(currentStep)

  return (
    <div className={`flex items-center justify-center space-x-4 ${className ?? ''}`}>
      {allSteps.map((step, index) => (
        <div
          key={index}
          onClick={() => onClickStep?.(step)}
          className={`w-3 h-3 rounded-full ${index <= currentStepIndex ? 'bg-color-8' : 'bg-color-3'}`}
        >
        </div>
      ))}
    </div>
  )
}
